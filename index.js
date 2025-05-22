const express = require('express');
const app = express();
const crypto = require('crypto');
const cors = require('cors');

app.use(cors());
app.use(express.json());

let validTokens = {};

// ძველი ტოკენების გაწმენდა ყოველ წუთში
setInterval(() => {
    const now = Date.now();
    const expireTime = 1 * 60 * 1000; // 1 წუთი
    
    Object.keys(validTokens).forEach(token => {
        if (now - validTokens[token].created > expireTime) {
            delete validTokens[token];
        }
    });
}, 60 * 1000);

app.get('/get-token', (req, res) => {
    try {
        const token = crypto.randomBytes(16).toString('hex');
        validTokens[token] = { 
            used: false, 
            created: Date.now(),
            ip: req.ip || req.connection.remoteAddress
        };
        
        console.log(`Generated token ${token} for IP ${req.ip}`);
        res.json({ token });
    } catch (error) {
        console.error('Error generating token:', error);
        res.status(500).json({ error: 'Failed to generate token' });
    }
});

app.get('/check-token/:token', (req, res) => {
    try {
        const { token } = req.params;

        if (!token) {
            return res.json({ valid: false, reason: 'No token provided' });
        }

        const tokenData = validTokens[token];

        if (!tokenData) {
            console.log(`Token ${token} not found`);
            return res.json({ valid: false, reason: 'Token not found' });
        }

        // ვადა ხომ არ გაუვიდა
        const now = Date.now();
        const isExpired = now - tokenData.created > 1 * 60 * 1000; // 1 წუთი

        if (isExpired) {
            delete validTokens[token];
            console.log(`Token ${token} expired`);
            return res.json({ valid: false, reason: 'Token expired' });
        }

        // თუ უკვე გამოყენებულია, დააბრუნე უარყოფითი პასუხი
        if (tokenData.used) {
            console.log(`Token ${token} already used`);
            return res.json({ valid: false, reason: 'Token already used' });
        }

        // მონიშნე როგორც გამოყენებული და წაშალე ტოკენი დაუყოვნებლივ
        tokenData.used = true;
        delete validTokens[token];

        console.log(`Token ${token} validated and deleted`);
        return res.json({ valid: true });

    } catch (error) {
        console.error('Error checking token:', error);
        return res.status(500).json({ valid: false, reason: 'Server error' });
    }
});


// ჯანმრთელობის შემოწმება
app.get('/health', (req, res) => {
    res.json({ 
        status: 'OK', 
        activeTokens: Object.keys(validTokens).length,
        timestamp: new Date().toISOString()
    });
});

const PORT = process.env.PORT || 5000;
app.listen(PORT, () => {
    console.log(`Token API running on port ${PORT}`);
    console.log(`Health check available at http://localhost:${PORT}/health`);
});
