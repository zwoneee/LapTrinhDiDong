"use strict";

// ==================== LẤY TOKEN ADMIN ====================
const token = localStorage.getItem("authToken");
if (!token) {
    alert("Bạn cần đăng nhập lại với quyền admin!");
}

// ==================== SIGNALR CONNECTION ====================
const connection = new signalR.HubConnectionBuilder()
    .withUrl("https://localhost:7068/chathub", { accessTokenFactory: () => token })
    .withAutomaticReconnect()
    .build();

let currentUserId = null;
let adminId = null;

// ==================== GET ADMIN ID FROM JWT ====================
function parseJwt(token) {
    try {
        const base64Url = token.split('.')[1];
        const base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/');
        const jsonPayload = decodeURIComponent(atob(base64).split('').map(c => {
            return '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2);
        }).join(''));
        return JSON.parse(jsonPayload);
    } catch {
        return null;
    }
}

// Lấy payload từ token
const payload = parseJwt(token);

// ✅ Lấy role đúng và xác định adminId
const roles = payload["http://schemas.microsoft.com/ws/2008/06/identity/claims/role"] || [];
if (roles.includes("Admin")) {
    adminId = parseInt(payload["http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier"] || 1);
} else {
    alert("Token không hợp lệ hoặc không phải admin!");
}


// ==================== LOAD USER LIST ====================
async function loadUserList() {
    try {
        const res = await fetch("https://localhost:7068/api/chat/users", {
            headers: { "Authorization": "Bearer " + token }
        });
        if (!res.ok) throw new Error("HTTP " + res.status);
        const users = await res.json();

        const listDiv = document.getElementById("userList");
        listDiv.innerHTML = "";
        users.forEach(u => {
            const div = document.createElement("div");
            div.className = "p-2 border-bottom user-item";
            div.style.cursor = "pointer";
            div.textContent = u.email;
            div.onclick = () => openChat(u.id, u.email);
            listDiv.appendChild(div);
        });
    } catch (err) {
        console.error("❌ Không thể tải danh sách user:", err);
    }
}

// ==================== OPEN CHAT ====================
async function openChat(userId, email) {
    currentUserId = userId;
    document.getElementById("chatTitle").textContent = "💬 Chat với " + email;
    document.getElementById("chatMessages").innerHTML = "";

    await loadChatHistory(userId);

    localStorage.setItem("currentChatUserId", userId);
    localStorage.setItem("currentChatUserEmail", email);
}

// ==================== LOAD CHAT HISTORY ====================
async function loadChatHistory(userId) {
    try {
        const res = await fetch(`https://localhost:7068/api/chat/history?withUserId=${userId}`, {
            headers: { "Authorization": "Bearer " + token }
        });
        if (!res.ok) throw new Error("HTTP " + res.status);
        const messages = await res.json();

        console.log("💬 Lịch sử user:", messages);

        const chatDiv = document.getElementById("chatMessages");
        chatDiv.innerHTML = "";

        messages.forEach(msg => {
            const senderId = msg.senderI ?? msg.fromUserId;
            const sender = (senderId === adminId) ? "Admin" : "User";
            appendMessage(sender, msg.content, msg.sentAt);
        });

        scrollToBottom();
    } catch (err) {
        console.error("❌ Lỗi tải lịch sử chat:", err);
    }
}

// ==================== APPEND MESSAGE ====================
function appendMessage(sender, content, time = null) {
    const div = document.createElement("div");
    const now = time ? new Date(time) : new Date();
    const timeStr = now.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit', second: '2-digit' });
    div.className = sender === "Admin" ? "text-end mb-2" : "text-start mb-2";
    div.innerHTML = `
        <div class="d-inline-block px-2 py-1 rounded ${sender === "Admin" ? "bg-primary text-white" : "bg-light"}">
            <strong>${sender}:</strong> ${content}
        </div>
        <div class="text-muted" style="font-size:0.8em;">${timeStr}</div>
    `;
    document.getElementById("chatMessages").appendChild(div);
}

function scrollToBottom() {
    const div = document.getElementById("chatMessages");
    div.scrollTop = div.scrollHeight;
}

// ==================== SEND MESSAGE ====================
async function sendAdminMessage() {
    const input = document.getElementById("chatMessageInput");
    const message = input.value.trim();
    if (!message || !currentUserId) return;

    try {
        const payload = { FromUserId: adminId, ToUserId: currentUserId, Content: message };

        const res = await fetch("https://localhost:7068/api/chat/admin/send", {
            method: "POST",
            headers: {
                "Content-Type": "application/json",
                "Authorization": `Bearer ${token}`
            },
            body: JSON.stringify(payload)
        });

        if (res.ok) {
            const data = await res.json();
            appendMessage("Admin", data.content, data.sentAt);
            scrollToBottom();
            input.value = "";
        }
    } catch (err) {
        console.error("❌ Gửi tin nhắn lỗi:", err);
    }
}

// ==================== RECEIVE MESSAGE REALTIME ====================
connection.on("ReceiveMessage", (message) => {
    const fromId = message.senderI ?? message.fromUserId;
    const toId = message.toUserId;

    if (fromId === adminId && toId === currentUserId) {
        appendMessage("Admin", message.content, message.sentAt);
        scrollToBottom();
    } else if (fromId === currentUserId) {
        appendMessage("User", message.content, message.sentAt);
        scrollToBottom();
    } else {
        console.log(`📨 Tin mới từ user #${fromId}`);
    }
});

// ==================== KẾT NỐI SIGNALR ====================
connection.start()
    .then(() => {
        console.log("✅ Admin connected to SignalR");
        loadUserList();

        const lastUserId = localStorage.getItem("currentChatUserId");
        const lastEmail = localStorage.getItem("currentChatUserEmail");
        if (lastUserId && lastEmail) {
            openChat(parseInt(lastUserId), lastEmail);
        }
    })
    .catch(err => console.error("❌ SignalR error:", err));

// ==================== EVENTS ====================
document.getElementById("sendChatMessage").addEventListener("click", sendAdminMessage);
document.getElementById("chatMessageInput").addEventListener("keydown", e => {
    if (e.key === "Enter") sendAdminMessage();
});
