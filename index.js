const express = require('express');
const app = express();
const cors = require('cors');
const crypto = require('crypto');

app.use(express.json());
app.use(cors());

const sessions = new Map();

app.post('/register-session', (req, res) => {
    const session = crypto.randomBytes(24).toString('hex');
    const ip = req.headers['x-forwarded-for'] || req.connection.remoteAddress;
    sessions.set(session, {
        ip,
        created: Date.now(),
        used: false
    });
    res.json({session});
});

// სერვერი ამოწმებს session-id-ს
app.get('/check-session/:session', (req, res) => {
    const session = req.params.session;
    const ip = req.headers['x-forwarded-for'] || req.connection.remoteAddress;
    const data = sessions.get(session);
    if (!data || data.used || Date.now() - data.created > 2*60*1000) {
        return res.json({valid: false});
    }
    if (data.ip !== ip) {
        return res.json({valid: false});
    }
    data.used = true; // ინვალიდაცია!
    sessions.set(session, data);
    return res.json({valid: true});
});

app.listen(8080, () => console.log("API listening on 8080"));
