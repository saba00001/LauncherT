const express = require('express');
const app = express();
const crypto = require('crypto');
const cors = require('cors');

app.use(cors());
app.use(express.json());

let activeTokens = {};
let authorizedSerials = {};

// ძველი ტოკენების გაწმენდა
setInterval(() => {
    const now = Date.now();
    const expireTime = 2 * 60 * 1000; // 2 წუთი
    
    Object.keys(activeTokens).forEach(token => {
        if (now - activeTokens[token].created > expireTime) {
            delete activeTokens[token];
        }
    });

    Object.keys(authorizedSerials).forEach(serial => {
        if (now - authorizedSerials[serial] > 10 * 60 * 1000) {
            delete authorizedSerials[serial];
        }
    });
}, 60 * 1000);

// ტოკენის გენერაცია სერიალით (თუ შესაძლებელია)
app.get('/get-token', (req, res) => {
    try {
        // სერიალი შეიძლება გადმოიცეს query-ში (ლაუნჩერიდან გააგზავნე ?serial=...)
        const playerSerial = req.query.serial;
        let token;

        // თუ სერიალი გადმოეცა, მოძებნე უკვე არსებული ტოკენი
        if (playerSerial) {
            for (const t in activeTokens) {
                if (activeTokens[t].serial === playerSerial && !activeTokens[t].used) {
                    token = t;
                    break;
                }
            }
        }

        // თუ არ მოიძებნა, გენერაცია
        if (!token) {
            token = crypto.randomBytes(20).toString('hex');
            activeTokens[token] = {
                created: Date.now(),
                serial: playerSerial || null,
                ip: req.ip,
                used: false
            };
        }

        res.json({ token, expires_in: 120 });
    } catch (error) {
        res.status(500).json({ error: 'Failed to generate token' });
    }
});
