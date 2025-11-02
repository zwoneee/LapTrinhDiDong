"use strict";

let connection = null;
let isChatInitialized = false;
const appendedMessageIds = new Set();
window.selectedFile = null;

// ====================== KHỞI CHẠY ======================
$(document).ready(() => {
    initUserChat();

    // fallback nếu load chậm
    setTimeout(() => {
        if (typeof loadChatHistory === "function") {
            console.log("🧩 Force load chat history sau 1s");
            loadChatHistory();
        }
    }, 1000);
});

// ====================== CHÍNH ======================
function initUserChat() {
    console.log("🧠 initUserChat() chạy");

    if (isChatInitialized) {
        console.log("⚠️ Chat đã khởi tạo rồi, return sớm");
        return;
    }
    isChatInitialized = true;

    const messagesDiv = document.getElementById("chatMessages");
    const input = document.getElementById("chatMessageInput");
    const sendBtn = document.getElementById("sendChatMessage");
    const fileInput = document.getElementById("chatFileInput");

    if (!messagesDiv || !input || !sendBtn) {
        console.error("❌ Không tìm thấy phần tử giao diện chat.");
        return;
    }

    const currentUser = window.currentUser || {};
    const token = localStorage.getItem("authToken");
    if (!currentUser.id || !token) {
        alert("❌ Bạn cần đăng nhập để sử dụng chat!");
        return;
    }

    // ====================== SIGNALR ======================
    console.log("⚙️ Đang khởi tạo kết nối SignalR...");
    connection = new signalR.HubConnectionBuilder()
        .withUrl("https://localhost:7068/chathub", {
            accessTokenFactory: () => token
        })
        .withAutomaticReconnect()
        .configureLogging(signalR.LogLevel.Information)
        .build();

    // Khi nhận tin nhắn mới
    connection.on("ReceiveMessage", (payload) => {
        try {
            console.log("🔔 Nhận tin nhắn:", payload);

            const fromId = payload.fromUserId ?? payload.FromUserId;
            const content = payload.content ?? payload.Content ?? "";
            const sentAt = payload.sentAt ?? payload.SentAt;
            const fileUrl = payload.fileUrl ?? payload.FileUrl;
            const fileType = payload.fileType ?? payload.FileType;
            const fileName = payload.fileName ?? payload.FileName;
            const id = payload.id ?? payload.Id;

            if (id && appendedMessageIds.has(id)) return;
            if (id) appendedMessageIds.add(id);

            const senderName = (fromId === 1) ? "Hỗ trợ" : (currentUser.name || "Bạn");
            appendMessage(fromId, content, sentAt, fileUrl, fileType, fileName, senderName);
        } catch (err) {
            console.error("❌ Lỗi khi xử lý ReceiveMessage:", err);
        }
    });

    connection.start()
        .then(() => {
            console.log("✅ SignalR connected, load chat history...");
            loadChatHistory();
        })
        .catch(err => {
            console.error("❌ Lỗi kết nối SignalR:", err);
        });

    // ====================== GỬI TIN NHẮN ======================
    sendBtn.addEventListener("click", sendMessage);
    input.addEventListener("keydown", (e) => {
        if (e.key === "Enter") sendMessage();
    });

    async function sendMessage() {
        const msg = input.value.trim();
        const file = window.selectedFile;
        if (!msg && !file) return;

        let uploadData = null;
        if (file) uploadData = await uploadFile(file);

        const payload = {
            FromUserId: parseInt(currentUser.id),
            ToUserId: 1, // gửi đến admin
            Content: msg || "",
            FileUrl: uploadData?.url ?? null,
            FileName: uploadData?.fileName ?? null,
            FileType: uploadData?.fileType ?? null
        };

        try {
            console.log("➡️ POST /api/chat/customer/send payload:", payload);
            const res = await fetch("https://localhost:7068/api/chat/customer/send", {
                method: "POST",
                headers: {
                    "Authorization": `Bearer ${token}`,
                    "Content-Type": "application/json"
                },
                body: JSON.stringify(payload)
            });

            if (!res.ok) {
                console.error("❌ Gửi tin thất bại:", res.status);
                return;
            }

            appendMessage(currentUser.id, msg, new Date().toISOString(), uploadData?.url, uploadData?.fileType, uploadData?.fileName, "Bạn");
            input.value = "";
            resetFileInput(fileInput);
        } catch (err) {
            console.error("❌ Gửi tin thất bại (network):", err);
        }
    }

    // ====================== LOAD HISTORY ======================
    async function loadChatHistory() {
        console.log("🔄 Đang tải lịch sử chat với Admin...");

        try {
            const url = `https://localhost:7068/api/chat/history?withUserId=1`;
            const res = await fetch(url, {
                method: "GET",
                headers: {
                    "Authorization": `Bearer ${token}`,
                    "Content-Type": "application/json"
                }
            });

            if (!res.ok) {
                console.error("❌ Lỗi tải lịch sử:", res.status);
                return;
            }

            const messages = await res.json();
            console.log("✅ Lịch sử tin nhắn:", messages);

            renderMessages(messages);
        } catch (err) {
            console.error("⚠️ Lỗi khi tải lịch sử:", err);
        }
    }

    window.loadChatHistory = loadChatHistory;

    // ====================== RENDER MESSAGES ======================
    function renderMessages(messages) {
        messagesDiv.innerHTML = "";
        messages.forEach(msg => {
            const id = msg.id ?? msg.Id;
            if (id && appendedMessageIds.has(id)) return;
            if (id) appendedMessageIds.add(id);

            const fromUserId = msg.fromUserId ?? msg.FromUserId;
            const senderName = (fromUserId === 1) ? "Hỗ trợ" : (currentUser.name || "Bạn");

            appendMessage(
                fromUserId,
                msg.content ?? msg.Content ?? "",
                msg.sentAt ?? msg.SentAt,
                msg.fileUrl ?? msg.FileUrl,
                msg.fileType ?? msg.FileType,
                msg.fileName ?? msg.FileName,
                senderName
            );
        });

        messagesDiv.scrollTop = messagesDiv.scrollHeight;
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
            console.error("❌ Upload thất bại:", res.status);
            return null;
        }

        return await res.json();
    }

    // ====================== HIỂN THỊ MỘT TIN ======================
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

    function resetFileInput(fileInput) {
        if (!fileInput) return;
        fileInput.value = "";
        window.selectedFile = null;
    }
}

// ====================== HELPER ======================
function escapeHtml(unsafe) {
    return String(unsafe)
        .replace(/&/g, "&amp;")
        .replace(/</g, "&lt;")
        .replace(/>/g, "&gt;")
        .replace(/"/g, "&quot;")
        .replace(/'/g, "&#039;");
}
