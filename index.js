const express = require('express');
const app = express();
const crypto = require('crypto');
const cors = require('cors');

// Body parser for JSON and urlencoded
app.use(cors());
app.use(express.json());
app.use(express.urlencoded({ extended: true }));

let activeTokens = {};
let authorizedSerials = {};

// გაწმენდა ძველი ტოკენების და სერიალების
setInterval(() => {
    const now = Date.now();
    const expireTime = 2 * 60 * 1000; // 2 წუთი

    Object.keys(activeTokens).forEach(token => {
        if (
            activeTokens.hasOwnProperty(token) &&
            now - activeTokens[token].created > expireTime
        ) {
            delete activeTokens[token];
        }
    });

    Object.keys(authorizedSerials).forEach(serial => {
        if (
            authorizedSerials.hasOwnProperty(serial) &&
            now - authorizedSerials[serial] > 10 * 60 * 1000
        ) {
            delete authorizedSerials[serial];
        }
    });
}, 60 * 1000);

// უნიკალური ტოკენის გენერაცია
app.get('/get-token', (req, res) => {
    try {
        const token = crypto.randomBytes(20).toString('hex');
        activeTokens[token] = {
            created: Date.now(),
            ip: req.ip,
            used: false
        };
        res.json({ token, expires_in: 120 });
    } catch (error) {
        console.error("[/get-token] Error:", error);
        res.status(500).json({ error: 'Failed to generate token' });
    }
});

// მოთამაშის ვერიფიკაცია
app.post('/verify-player', (req, res) => {
    try {
        // ლოგირება მიღებული ბოდის
        console.log("[/verify-player] BODY RECEIVED:", req.body);
        console.log("[/verify-player] BODY TYPE:", typeof req.body);
        console.log("[/verify-player] IS ARRAY:", Array.isArray(req.body));

        let data;
        // Check if body is an array (MTA sends it wrapped in array)
        if (Array.isArray(req.body) && req.body.length > 0) {
            data = req.body[0]; // Take the first element if it's an array
            console.log("[/verify-player] Extracted from array:", data);
        } else {
            data = req.body; // Use directly if it's an object
        }

        const { token, serial, name } = data;
        console.log(`[VERIFY] Req: token=${token}, serial=${serial}, name=${name}`);

        if (!token || !serial) {
            console.log(`[VERIFY] ❌ Reject: Token or serial missing`);
            return res.status(400).json({ authorized: false, reason: 'ტოკენი ან სერიალი არ არის მითითებული' });
        }

        const tokenData = activeTokens[token];
        if (!tokenData) {
            console.log(`[VERIFY] ❌ Reject: Token not found or expired (${token})`);
            return res.status(400).json({ authorized: false, reason: 'ტოკენი ვერ მოიძებნა ან ვადა გაუვიდა' });
        }

        const now = Date.now();
        if (now - tokenData.created > 2 * 60 * 1000) {
            delete activeTokens[token];
            console.log(`[VERIFY] ❌ Reject: Token expired (${token})`);
            return res.status(400).json({ authorized: false, reason: 'ტოკენის ვადა ამოიწურა' });
        }

        if (tokenData.used) {
            delete activeTokens[token];
            console.log(`[VERIFY] ❌ Reject: Token already used (${token})`);
            return res.status(400).json({ authorized: false, reason: 'ტოკენი უკვე გამოყენებულია' });
        }

        tokenData.used = true;
        authorizedSerials[serial] = now;
        delete activeTokens[token];

        console.log(`[VERIFY] ✅ Authorized: ${serial} (${name})`);
        res.json({ authorized: true, message: 'ავტორიზაცია წარმატებული' });

    } catch (error) {
        console.error("[/verify-player] Error:", error);
        res.status(500).json({ authorized: false, reason: 'სერვერის შეცდომა' });
    }
});

// სტატისტიკა
app.get('/stats', (req, res) => {
    res.json({
        active_tokens: Object.keys(activeTokens).length,
        authorized_serials: Object.keys(authorizedSerials).length,
        timestamp: new Date().toISOString()
    });
});

// ჯანმრთელობის შემოწმება
app.get('/health', (req, res) => {
    res.json({
        status: 'OK',
        uptime: process.uptime(),
        timestamp: new Date().toISOString()
    });
});

const PORT = process.env.PORT || 5000;
app.listen(PORT, () => {
    console.log(`🚀 Token API running on port ${PORT}`);
});
