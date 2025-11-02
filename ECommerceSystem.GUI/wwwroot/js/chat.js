"use strict";

let connection = null;
let isChatInitialized = false;
const appendedMessageIds = new Set();

function initUserChat() {
    if (isChatInitialized) return;
    isChatInitialized = true;

    const messagesDiv = document.getElementById("chatMessages");
    const input = document.getElementById("chatMessageInput");
    const sendBtn = document.getElementById("sendChatMessage");
    const fileInput = document.getElementById("chatFileInput");
    const currentUser = window.currentUser || {};

    if (!messagesDiv || !input || !sendBtn || !fileInput) {
        console.error("❌ Không tìm thấy phần tử chat");
        return;
    }

    if (!currentUser.id) {
        console.error("❌ currentUser.id không tồn tại, cần đăng nhập trước");
        return;
    }

    const token = localStorage.getItem("authToken");
    if (!token) {
        console.error("❌ Thiếu token đăng nhập");
        return;
    }

    // ====================== SIGNALR ======================
    connection = new signalR.HubConnectionBuilder()
        .withUrl("https://localhost:7068/chathub", {
            accessTokenFactory: () => token
        })
        .withAutomaticReconnect()
        .build();

    // Nhận tin nhắn mới từ Admin
    connection.on("ReceiveMessage", (fromUserId, message) => {
        console.log("📩 ReceiveMessage:", { fromUserId, message });
        const senderName = (parseInt(fromUserId) === 1) ? "Hỗ trợ" : "Bạn";
        appendMessage(fromUserId, message, new Date(), null, null, null, senderName);
    });

    // Kết nối thành công
    connection.start()
        .then(() => {
            console.log("✅ SignalR connected");
            loadChatHistory(); // <-- gọi ngay khi kết nối xong
        })
        .catch(err => console.error("❌ Lỗi kết nối SignalR:", err));

    // ====================== GỬI TIN ======================
    async function sendMessage() {
        const msg = input.value.trim();
        if (!msg) return;

        const payload = {
            FromUserId: parseInt(currentUser.id),
            ToUserId: 1, // admin luôn có ID=1
            Content: msg
        };

        try {
            const res = await fetch("https://localhost:7068/api/chat/customer/send", {
                method: "POST",
                headers: {
                    "Content-Type": "application/json",
                    "Authorization": `Bearer ${token}`
                },
                body: JSON.stringify(payload)
            });

            if (!res.ok) {
                console.error("❌ Gửi tin thất bại:", res.status);
                return;
            }

            appendMessage(currentUser.id, msg, new Date(), null, null, null, "Bạn");
            input.value = "";
        } catch (err) {
            console.error("❌ Lỗi khi gửi tin:", err);
        }
    }

    sendBtn.addEventListener("click", sendMessage);
    input.addEventListener("keydown", e => { if (e.key === "Enter") sendMessage(); });

    // ====================== TẢI LỊCH SỬ ======================
    async function loadChatHistory() {
        const url = `https://localhost:7068/api/chat/history?withUserId=1`;
        console.log("🔄 GET", url);

        try {
            const res = await fetch(url, {
                headers: { "Authorization": `Bearer ${token}` }
            });

            if (!res.ok) {
                const text = await res.text();
                console.error("❌ Lỗi tải lịch sử:", res.status, text);
                return;
            }

            const data = await res.json();
            console.log("✅ Lịch sử:", data);
            renderMessages(data);
        } catch (err) {
            console.error("❌ Lỗi khi tải lịch sử:", err);
        }
    }

    function renderMessages(messages) {
        messagesDiv.innerHTML = "";
        messages.forEach(msg => {
            const id = msg.id ?? msg.Id;
            if (id && appendedMessageIds.has(id)) return;
            if (id) appendedMessageIds.add(id);

            const fromId = msg.fromUserId ?? msg.FromUserId;
            const senderName = (parseInt(fromId) === 1) ? "Hỗ trợ" : "Bạn";

            appendMessage(
                fromId,
                msg.content ?? msg.Content,
                msg.sentAt ?? msg.SentAt,
                msg.fileUrl ?? msg.FileUrl,
                msg.fileType ?? msg.FileType,
                msg.fileName ?? msg.FileName,
                senderName
            );
        });
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
}

$(document).ready(() => {
    initUserChat();
    setTimeout(() => {
        if (typeof loadChatHistory === "function") loadChatHistory();
    }, 1000);
});

function escapeHtml(unsafe) {
    return String(unsafe)
        .replace(/&/g, "&amp;")
        .replace(/</g, "&lt;")
        .replace(/>/g, "&gt;")
        .replace(/"/g, "&quot;")
        .replace(/'/g, "&#039;");
}
