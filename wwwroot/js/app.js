document.addEventListener("DOMContentLoaded", () => {
    const loginForm = document.getElementById("login-form");
    const registerForm = document.getElementById("register-form");
    const chatSection = document.getElementById("chat-section");
    const messagesDiv = document.getElementById("messages");
    const chatInput = document.getElementById("chat-input");
    const sendMessageBtn = document.getElementById("send-message");
    const recipientInput = document.getElementById("recipient-id"); // For specifying the recipient

    let authToken = ""; // JWT token
    let connection = null;

    // Login Form Submission
    loginForm.addEventListener("submit", async (event) => {
        event.preventDefault();
        const phoneNumber = document.getElementById("login-phone").value;
        const password = document.getElementById("login-password").value;

        try {
            const response = await fetch("http://localhost:5000/api/user/login", {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({ phoneNumber, password }),
            });
            const result = await response.json();
            if (response.ok) {
                authToken = result.Token; // Store the JWT token
                alert("Login successful!");
                loginForm.style.display = "none"; // Hide login form
                registerForm.style.display = "none"; // Hide register form
                chatSection.hidden = false; // Show chat section
                await startSignalRConnection(); // Start SignalR connection
            } else {
                alert(result || "Login failed.");
            }
        } catch (error) {
            console.error("Error logging in:", error);
        }
    });

    // Register Form Submission
    registerForm.addEventListener("submit", async (event) => {
        event.preventDefault();
        const phoneNumber = document.getElementById("register-phone").value;
        const name = document.getElementById("register-name").value;
        const password = document.getElementById("register-password").value;

        try {
            const response = await fetch("http://localhost:5000/api/user/register", {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({ phoneNumber, name, password }),
            });
            const result = await response.json();
            if (response.ok) {
                alert(result.Message || "Registration successful!");
            } else {
                alert(result || "Registration failed.");
            }
        } catch (error) {
            console.error("Error registering:", error);
        }
    });

    // SignalR Connection Setup
    async function startSignalRConnection() {
        connection = new signalR.HubConnectionBuilder()
            .withUrl("http://localhost:5000/chathub", {
                accessTokenFactory: () => authToken, // Attach the JWT token for authentication
            })
            .withAutomaticReconnect()
            .build();

        // Listen for incoming messages
        connection.on("ReceiveMessage", (sender, message) => {
            const newMessage = document.createElement("div");
            newMessage.textContent = `${sender}: ${message}`;
            messagesDiv.appendChild(newMessage);
        });

        // Start the SignalR connection
        try {
            await connection.start();
            console.log("SignalR Connected");
        } catch (err) {
            console.error("SignalR Connection Error:", err);
        }
    }

    // Send Message to Recipient
    sendMessageBtn.addEventListener("click", async () => {
        const messageContent = chatInput.value;
        const recipient = recipientInput.value; // Get recipient ID from input

        if (!connection) {
            alert("SignalR connection not established!");
            return;
        }

        if (!recipient) {
            alert("Please enter a recipient ID!");
            return;
        }

        try {
            // Invoke the SignalR method to send the message
            await connection.invoke("SendMessageToUser", recipient, messageContent);

            const newMessage = document.createElement("div");
            newMessage.textContent = `You to ${recipient}: ${messageContent}`;
            messagesDiv.appendChild(newMessage);

            chatInput.value = ""; // Clear the input field
        } catch (error) {
            console.error("Error sending message:", error);
        }
    });
});