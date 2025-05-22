const express = require('express');
const app = express();
const crypto = require('crypto');
const cors = require('cors');

app.use(cors());

let validTokens = {};

app.get('/get-token', (req, res) => {
    const token = crypto.randomBytes(16).toString('hex');
    validTokens[token] = { used: false, created: Date.now() };
    res.json({ token });
});

app.get('/check-token/:token', (req, res) => {
    const { token } = req.params;
    if (validTokens[token] && !validTokens[token].used) {
        validTokens[token].used = true;
        res.json({ valid: true });
    } else {
        res.json({ valid: false });
    }
});

app.listen(5000, () => console.log('Token API on port 5000'));