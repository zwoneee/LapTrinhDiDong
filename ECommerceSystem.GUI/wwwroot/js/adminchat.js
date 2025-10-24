"use strict";

let connection = null;
let selectedUserId = null;
let adminUser = { id: 1, name: "Admin" }; // ✅ hoặc lấy từ token
let token = localStorage.getItem("authToken");

async function initAdminChat() {
    console.log("📨 AdminChat loaded");

    if (!token) {
        console.error("❌ Không tìm thấy token, hãy đăng nhập lại");
        return;
    }

    // ====================== ⚡ SignalR ======================
    connection = new signalR.HubConnectionBuilder()
        .withUrl("https://localhost:7068/chathub", {
            accessTokenFactory: () => token
        })
        .withAutomaticReconnect()
        .configureLogging(signalR.LogLevel.Information)
        .build();

    // Nhận tin nhắn realtime từ user
    connection.on("ReceiveMessage", (fromId, message, sentAt, fileUrl, fileType, fileName) => {
        if (selectedUserId && parseInt(fromId) === parseInt(selectedUserId)) {
            appendMessage(fromId, message, sentAt, fileUrl, fileType, fileName);
        } else {
            showNotification(fromId, message);
        }
    });

    // Kết nối
    connection.start()
        .then(() => {
            console.log("✅ Admin SignalR connected");
            loadUsers();
        })
        .catch(err => console.error("❌ Lỗi kết nối SignalR:", err));
}

// ====================== 👥 Load danh sách user ======================
async function loadUsers() {
    try {
        const response = await fetch("https://localhost:7068/api/chat/users", {
            headers: { "Authorization": `Bearer ${token}` }
        });

        if (!response.ok) {
            console.error("❌ Không thể tải danh sách user:", response.status);
            return;
        }

        const users = await response.json();
        console.log("👥 Users:", users);

        const listDiv = document.getElementById("userList");
        listDiv.innerHTML = "";

        users.forEach(u => {
            const div = document.createElement("div");
            div.className = "user-item p-2 border-bottom cursor-pointer";
            div.style.cursor = "pointer";
            div.textContent = u.email || u.Email;
            div.onclick = () => selectUser(u.id || u.Id);
            listDiv.appendChild(div);
        });
    } catch (err) {
        console.error("Lỗi tải danh sách người dùng:", err);
    }
}

// ====================== 💬 Khi chọn user ======================
async function selectUser(userId) {
    selectedUserId = userId;
    document.getElementById("chatMessages").innerHTML = "";
    document.getElementById("chatTitle").textContent = "Chat với User #" + userId;

    await loadChatHistory(userId);
}

// ====================== 📜 Lịch sử chat ======================
async function loadChatHistory(userId) {
    try {
        const res = await fetch(`https://localhost:7068/api/chat/history?withUserId=${userId}`, {
            headers: { "Authorization": `Bearer ${token}` }
        });
        if (!res.ok) {
            console.error("❌ Lỗi tải lịch sử:", res.status);
            return;
        }

        const messages = await res.json();
        const filtered = messages.filter(m =>
            m.fromUserId === userId || m.toUserId === userId
        );

        console.log("💬 Lịch sử user:", filtered);
        renderMessages(filtered);
    } catch (err) {
        console.error("⚠️ Lỗi khi tải lịch sử:", err);
    }
}

function renderMessages(messages) {
    const chatBox = document.getElementById("chatMessages");
    chatBox.innerHTML = "";

    messages.forEach(msg => {
        appendMessage(msg.senderI, msg.content, msg.sentAt, msg.fileUrl, msg.fileType, msg.fileName);
    });
}

// ====================== 📨 Gửi tin ======================
async function sendAdminMessage() {
    const input = document.getElementById("chatMessageInput");
    const msg = input.value.trim();
    if (!msg || !selectedUserId) return;

    const payload = {
        FromUserId: adminUser.id,
        ToUserId: selectedUserId,
        Content: msg
    };

    // Gửi qua SignalR
    if (connection && connection.state === "Connected") {
        try {
            await connection.invoke("SendMessageFromAdmin", payload);
            appendMessage(adminUser.id, msg, new Date().toISOString());
            input.value = "";
        } catch (err) {
            console.warn("⚠️ SignalR lỗi, fallback API:", err);
            await sendMessageFallback(payload);
        }
    } else {
        await sendMessageFallback(payload);
    }
}

// Fallback gửi API
async function sendMessageFallback(payload) {
    try {
        const res = await fetch("https://localhost:7068/api/admin/chat/send", {
            method: "POST",
            headers: {
                "Authorization": `Bearer ${token}`,
                "Content-Type": "application/json"
            },
            body: JSON.stringify(payload)
        });

        if (!res.ok) throw new Error("HTTP " + res.status);
        appendMessage(adminUser.id, payload.Content, new Date().toISOString());
    } catch (err) {
        console.error("❌ Gửi tin API thất bại:", err);
    }
}

// ====================== 💬 Append tin ======================
function appendMessage(fromId, message, sentAt, fileUrl, fileType, fileName) {
    const chatBox = document.getElementById("chatMessages");
    const isMe = parseInt(fromId) === parseInt(adminUser.id);
    const time = new Date(sentAt).toLocaleTimeString();

    const div = document.createElement("div");
    div.className = isMe ? "text-end my-1" : "text-start my-1";
    div.innerHTML = `
        <div style="display:inline-block; background:${isMe ? "#007bff" : "#e9ecef"}; color:${isMe ? "white" : "black"}; padding:6px 10px; border-radius:10px; max-width:80%;">
            ${isMe ? "Admin" : "User"}: ${message}
        </div>
        <div style="font-size:10px; color:gray;">${time}</div>
    `;

    chatBox.appendChild(div);
    chatBox.scrollTop = chatBox.scrollHeight;
}

// ====================== 🔔 Thông báo user khác ======================
function showNotification(userId, message) {
    const listDiv = document.getElementById("userList");
    const userItem = [...listDiv.children].find(el => el.textContent.includes(userId));
    if (userItem) {
        userItem.style.backgroundColor = "#ffeeba";
        setTimeout(() => userItem.style.backgroundColor = "", 2000);
    }
}

document.addEventListener("DOMContentLoaded", initAdminChat);
