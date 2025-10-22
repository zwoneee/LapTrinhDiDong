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
    console.log("🚀 Token hiện tại:", token);

    // ====================== SIGNALR ======================
    connection = new signalR.HubConnectionBuilder()
        .withUrl("https://localhost:7068/chathub", { accessTokenFactory: () => token })
        .withAutomaticReconnect()
        .configureLogging(signalR.LogLevel.Information)
        .build();

    connection.on("ReceiveMessage", (fromId, message, sentAt, fileUrl, fileType, fileName) => {
        appendMessage(fromId, message, sentAt, fileUrl, fileType, fileName);
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
            const response = await fetch("https://localhost:7068/api/chat/history", {
                method: "GET",
                headers: {
                    "Authorization": `Bearer ${token}`,
                    "Content-Type": "application/json"
                }
            });

            if (!response.ok) {
                console.error(`❌ Không thể tải lịch sử: ${response.status}`);
                return;
            }

            const messages = await response.json();
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
                msg.content,
                msg.sentAt,
                msg.fileUrl,
                msg.fileType,
                msg.fileName,
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

        try {
            const res = await fetch("https://localhost:7068/api/chat/upload", {
                method: "POST",
                body: formData,
                headers: {
                    "Authorization": `Bearer ${token}`
                }
            });

            if (!res.ok) {
                const text = await res.text();
                console.error("❌ Upload failed:", res.status, text);
                throw new Error("Upload failed");
            }

            const data = await res.json();
            console.log("✅ Upload thành công:", data);
            return data;
        } catch (err) {
            console.error("❌ Upload file thất bại:", err);
            return null;
        }
    }

    // ====================== SEND MESSAGE ======================
    async function sendMessage() {
        const msg = input.value.trim();
        const file = window.selectedFile;

        if (!msg && !file) return;

        if (file) {
            const result = await uploadFile(file);
            if (!result) return;
            window.selectedFileUrl = result.url;
            window.selectedFileName = result.fileName;
            window.selectedFileType = result.fileType;
        }

        const payload = {
            FromUserId: parseInt(currentUser.id),
            ToUserId: 1,
            Content: msg || "",
            FileUrl: window.selectedFileUrl || null,
            FileName: window.selectedFileName || null,
            FileType: window.selectedFileType || null
        };

        if (connection && connection.state === "Connected") {
            connection.invoke("SendMessageFromCustomer", payload)
                .then(() => {
                    appendMessage(currentUser.id, payload.Content, new Date().toISOString(),
                        payload.FileUrl, payload.FileType, payload.FileName);
                    resetInput();
                })
                .catch(err => {
                    console.warn("⚠️ SignalR lỗi, fallback API:", err);
                    sendMessageFallback(payload);
                });
        } else {
            console.warn("⚠️ SignalR chưa kết nối, fallback API");
            sendMessageFallback(payload);
        }
    }

    // ====================== FALLBACK API ======================
    async function sendMessageFallback(payload) {
        try {
            const res = await fetch("https://localhost:7068/api/chat/customer/send", {
                method: "POST",
                headers: {
                    "Content-Type": "application/json",
                    "Authorization": `Bearer ${token}`
                },
                body: JSON.stringify(payload)
            });

            if (!res.ok) throw new Error("Server returned " + res.status);

            const data = await res.json().catch(() => ({ sentAt: new Date().toISOString() }));
            appendMessage(currentUser.id, payload.Content, data.sentAt || new Date().toISOString(),
                payload.FileUrl, payload.FileType, payload.FileName);
            resetInput();
        } catch (err) {
            console.error("❌ Gửi tin qua API thất bại:", err);
        }
    }

    // ====================== APPEND MESSAGE ======================
    function appendMessage(userId, message, sentAt, fileUrl, fileType, fileName, senderName) {
        const isMe = parseInt(userId) === parseInt(currentUser.id);
        const time = new Date(sentAt).toLocaleTimeString();
        const div = document.createElement("div");
        div.className = isMe ? "text-end my-1" : "text-start my-1";

        let fileHtml = "";
        if (fileUrl) {
            if (fileType === "image") {
                fileHtml = `<div><img src="${fileUrl}" style="max-width:150px; border-radius:6px;" /></div>`;
            } else if (fileType === "video") {
                fileHtml = `<div><video src="${fileUrl}" controls style="max-width:200px; border-radius:6px;"></video></div>`;
            } else {
                fileHtml = `<div><a href="${fileUrl}" target="_blank">${fileName}</a></div>`;
            }
        }

        div.innerHTML = `
            <div style="display:inline-block; background:${isMe ? "#0d6efd" : "#e9ecef"};
                        color:${isMe ? "white" : "black"}; padding:6px 10px; border-radius:10px; max-width:80%;">
                <strong>${senderName || (isMe ? "Bạn" : "Hỗ trợ")}:</strong> ${message || ""}
                ${fileHtml}
            </div>
            <div style="font-size:10px; color:gray;">${time}</div>
        `;

        messagesDiv.appendChild(div);
        messagesDiv.scrollTop = messagesDiv.scrollHeight;
    }

    function resetInput() {
        input.value = "";
        window.selectedFile = null;
        window.selectedFileUrl = null;
        window.selectedFileType = null;
        window.selectedFileName = null;
        fileInput.value = "";
        input.focus();
    }

    // ====================== EVENT LISTENERS ======================
    sendBtn.addEventListener("click", sendMessage);
    input.addEventListener("keypress", e => { if (e.key === "Enter") sendMessage(); });
    fileInput.addEventListener("change", e => {
        const file = e.target.files[0];
        if (!file) return;
        window.selectedFile = file;
        console.log("📎 Đã chọn file:", file.name, file.type, file.size);
    });
}

$(document).ready(() => {
    console.log("💬 Chat JS ready!");
    initUserChat();
});
