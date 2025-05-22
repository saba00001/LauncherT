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
    const expireTime = 5 * 60 * 1000; // 5 წუთი
    
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
        
        if (tokenData.used) {
            console.log(`Token ${token} already used`);
            return res.json({ valid: false, reason: 'Token already used' });
        }
        
        // ტოკენის ვადის შემოწმება (5 წუთი)
        const now = Date.now();
        if (now - tokenData.created > 5 * 60 * 1000) {
            delete validTokens[token];
            console.log(`Token ${token} expired`);
            return res.json({ valid: false, reason: 'Token expired' });
        }
        
        // ტოკენის გამოყენება
        validTokens[token].used = true;
        validTokens[token].usedAt = now;
        
        console.log(`Token ${token} validated successfully`);
        res.json({ valid: true });
        
        // წაშალე ტოკენი 1 წუთის შემდეგ
        setTimeout(() => {
            delete validTokens[token];
        }, 60 * 1000);
        
    } catch (error) {
        console.error('Error checking token:', error);
        res.status(500).json({ valid: false, reason: 'Server error' });
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
