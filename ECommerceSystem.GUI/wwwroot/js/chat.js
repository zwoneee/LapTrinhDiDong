"use strict";

let connection = null;
let isChatInitialized = false;
window.selectedFile = null;
window.selectedFileUrl = null;
window.selectedFileType = null;
window.selectedFileName = null;

function initUserChat() {
    if (isChatInitialized) return;
    isChatInitialized = true;

    const messagesDiv = document.getElementById("chatMessages");
    const input = document.getElementById("chatMessageInput");
    const sendBtn = document.getElementById("sendChatMessage");
    const fileInput = document.getElementById("chatFileInput");

    if (!messagesDiv || !input || !sendBtn || !fileInput) return;

    const currentUser = window.currentUser || {};
    if (!currentUser.id) return;

    const token = localStorage.getItem("authToken");
    if (!token) {
        console.error("❌ Token không tồn tại. Hãy login trước.");
        return;
    }

    // ====================== SIGNALR ======================
    connection = new signalR.HubConnectionBuilder()
        .withUrl("https://localhost:7068/chathub", { accessTokenFactory: () => token })
        .withAutomaticReconnect()
        .configureLogging(signalR.LogLevel.Information)
        .build();

    connection.on("ReceiveMessage", (fromId, message, sentAt, fileUrl, fileType, fileName) => {
        const senderName = fromId === 1 ? "Hỗ trợ" : (currentUser.name || "Bạn");
        appendMessage(fromId, message, sentAt, fileUrl, fileType, fileName, senderName);
    });

    connection.start()
        .then(() => {
            console.log("✅ SignalR connected");
            loadChatHistory();
        })
        .catch(err => console.error("❌ SignalR connection error:", err));

    // ====================== LOAD HISTORY ======================
    async function loadChatHistory() {
        console.log("🔄 Đang tải lịch sử...");

        try {
            const url = "https://localhost:7068/api/chat/history";
            const res = await fetch(url, {
                method: "GET",
                headers: {
                    "Authorization": `Bearer ${token}`,
                    "Content-Type": "application/json"
                }
            });

            if (!res.ok) {
                const txt = await res.text().catch(() => "<no response body>");
                console.error(`❌ Không thể tải lịch sử: ${res.status}`, txt);
                return;
            }

            const contentType = res.headers.get("content-type") || "";
            if (!contentType.includes("application/json")) {
                const txt = await res.text().catch(() => "<no response body>");
                console.warn("⚠️ /api/chat/history returned non-JSON:", txt);
                return;
            }

            const messages = await res.json();
            console.log("✅ Lịch sử tin nhắn:", messages);
            renderMessages(messages);
        } catch (error) {
            console.error("⚠️ Lỗi khi tải lịch sử:", error);
        }
    }

    // ====================== RENDER MESSAGES ======================
    function renderMessages(messages) {
        const chatMessages = document.getElementById("chatMessages");
        chatMessages.innerHTML = "";

        messages.forEach(msg => {
            const senderName =
                msg.fromUserId === parseInt(currentUser.id)
                    ? currentUser.name || "Bạn"
                    : "Hỗ trợ";

            appendMessage(
                msg.fromUserId,
                msg.Content ?? msg.content ?? "",
                msg.SentAt ?? msg.sentAt,
                msg.FileUrl ?? msg.fileUrl,
                msg.FileType ?? msg.fileType,
                msg.FileName ?? msg.fileName,
                senderName
            );
        });

        chatMessages.scrollTop = chatMessages.scrollHeight;
    }

    // ====================== UPLOAD FILE ======================
    async function uploadFile(file) {
        if (!file) return null;

        const formData = new FormData();
        formData.append("file", file);

        const res = await fetch("https://localhost:7068/api/chat/upload", {
            method: "POST",
            body: formData,
            headers: { "Authorization": `Bearer ${token}` }
        });
        if (!res.ok) {
            const txt = await res.text().catch(() => "<no response body>");
            console.error("❌ Upload failed:", res.status, txt);
            return null;
        }

        const data = await res.json().catch(() => null);
        return data;
    }

    // ====================== SEND MESSAGE ======================
    async function sendMessage() {
        const msg = input.value.trim();
        const file = window.selectedFile;
        if (!msg && !file) return;

        let uploadData = null;
        if (file) uploadData = await uploadFile(file);

        // Ensure payload exactly matches ChatMessage model on server and always includes Content
        const payload = {
            FromUserId: parseInt(currentUser.id),
            ToUserId: 1,
            Content: msg || "", // important: always include Content (not undefined/null)
            FileUrl: uploadData?.url ?? null,
            FileName: uploadData?.fileName ?? null,
            FileType: uploadData?.fileType ?? null
        };

        try {
            console.log("➡️ POST /api/chat/customer/send payload:", payload);
            const res = await fetch("https://localhost:7068/api/chat/customer/send", {
                method: "POST",
                headers: {
                    "Content-Type": "application/json",
                    "Authorization": `Bearer ${token}`
                },
                body: JSON.stringify(payload)
            });

            // Read response body for debugging even on non-OK statuses
            const contentType = res.headers.get("content-type") || "";
            const resText = contentType.includes("application/json") ? await res.text().catch(() => "") : await res.text().catch(() => "");

            if (!res.ok) {
                console.error("❌ Gửi tin thất bại:", res.status, resText || "<empty body>");
                return;
            }

            // try parse JSON if possible
            let data = null;
            if (contentType.includes("application/json")) {
                try { data = JSON.parse(resText); } catch (e) { console.warn("⚠️ customer/send returned invalid JSON:", e); }
            }

            // Use server fields if available, otherwise fallback to local values
            const sentAt = data?.SentAt ?? data?.sentAt ?? new Date().toISOString();
            const fileUrl = data?.FileUrl ?? uploadData?.url ?? null;
            const fileType = data?.FileType ?? uploadData?.fileType ?? null;
            const fileName = data?.FileName ?? uploadData?.fileName ?? null;

            appendMessage(currentUser.id, payload.Content, sentAt, fileUrl, fileType, fileName, "Bạn");
            resetInput();
        } catch (err) {
            console.error("❌ Gửi tin thất bại (network):", err);
        }
    }

    // ====================== APPEND MESSAGE ======================
    function appendMessage(userId, message, sentAt, fileUrl, fileType, fileName, senderName) {
        const isMe = parseInt(userId) === parseInt(currentUser.id);
        const time = sentAt ? new Date(sentAt).toLocaleTimeString() : new Date().toLocaleTimeString();
        const div = document.createElement("div");
        div.className = isMe ? "text-end my-1" : "text-start my-1";

        let fileHtml = "";
        if (fileUrl) {
            if (fileType === "image") fileHtml = `<img src="${fileUrl}" style="max-width:150px;border-radius:6px;">`;
            else if (fileType === "video") fileHtml = `<video src="${fileUrl}" controls style="max-width:200px;border-radius:6px;"></video>`;
            else fileHtml = `<a href="${fileUrl}" target="_blank">${fileName ?? "file"}</a>`;
        }

        div.innerHTML = `
            <div style="display:inline-block;background:${isMe ? "#0d6efd" : "#e9ecef"};
                        color:${isMe ? "white" : "black"};padding:6px 10px;border-radius:10px;max-width:80%;">
                <strong>${senderName}:</strong> ${escapeHtml(message || "")} ${fileHtml}
            </div>
            <div style="font-size:10px;color:gray;">${time}</div>
        `;

        messagesDiv.appendChild(div);
        messagesDiv.scrollTop = messagesDiv.scrollHeight;
    }

    function resetInput() {
        input.value = "";
        fileInput.value = "";
        window.selectedFile = null;
    }

    // ====================== EVENTS ======================
    sendBtn.addEventListener("click", sendMessage);
    input.addEventListener("keypress", e => { if (e.key === "Enter") sendMessage(); });
    fileInput.addEventListener("change", e => window.selectedFile = e.target.files[0]);
}

$(document).ready(() => {
    initUserChat();
});

// small helper to avoid injecting raw HTML from messages
function escapeHtml(unsafe) {
    return String(unsafe)
        .replace(/&/g, "&amp;")
        .replace(/</g, "&lt;")
        .replace(/>/g, "&gt;")
        .replace(/"/g, "&quot;")
        .replace(/'/g, "&#039;");
}
