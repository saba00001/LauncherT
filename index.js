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
            console.log(`Token ${token} expired and removed`);
            delete activeTokens[token];
        }
    });
    
    // serial-ების გაწმენდა (10 წუთში)
    Object.keys(authorizedSerials).forEach(serial => {
        if (now - authorizedSerials[serial] > 10 * 60 * 1000) {
            delete authorizedSerials[serial];
        }
    });
}, 60 * 1000);

// ტოკენის გენერაცია ლაუნჩერისთვის
app.get('/get-token', (req, res) => {
    try {
        const token = crypto.randomBytes(20).toString('hex');
        const clientIP = req.ip || req.connection.remoteAddress || req.headers['x-forwarded-for'];
        
        activeTokens[token] = { 
            created: Date.now(),
            ip: clientIP,
            used: false
        };
        
        console.log(`🎫 Generated token ${token} for IP ${clientIP}`);
        res.json({ token, expires_in: 120 }); // 2 წუთი
    } catch (error) {
        console.error('❌ Error generating token:', error);
        res.status(500).json({ error: 'Failed to generate token' });
    }
});

// მოთამაშის ვერიფიკაცია სერვერიდან
app.post('/verify-player', (req, res) => {
    try {
        const { token, serial, name } = req.body;
        
        if (!token || !serial) {
            return res.json({ 
                authorized: false, 
                reason: 'ტოკენი ან სერიალი არ არის მითითებული' 
            });
        }
        
        const tokenData = activeTokens[token];
        
        if (!tokenData) {
            console.log(`❌ Token ${token} not found for ${name} (${serial})`);
            return res.json({ 
                authorized: false, 
                reason: 'ტოკენი ვერ მოიძებნა ან ვადა გაუვიდა' 
            });
        }
        
        // ვადის შემოწმება
        const now = Date.now();
        if (now - tokenData.created > 2 * 60 * 1000) {
            delete activeTokens[token];
            return res.json({ 
                authorized: false, 
                reason: 'ტოკენის ვადა ამოიწურა' 
            });
        }
        
        // თუ ტოკენი უკვე გამოყენებულია
        if (tokenData.used) {
            return res.json({ 
                authorized: false, 
                reason: 'ტოკენი უკვე გამოყენებულია' 
            });
        }
        
        // ავტორიზაცია
        tokenData.used = true;
        authorizedSerials[serial] = now;
        delete activeTokens[token]; // ტოკენის წაშლა
        
        console.log(`✅ Player ${name} (${serial}) authorized with token ${token}`);
        
        res.json({ 
            authorized: true,
            message: 'ავტორიზაცია წარმატებული'
        });
        
    } catch (error) {
        console.error('❌ Error verifying player:', error);
        res.status(500).json({ 
            authorized: false, 
            reason: 'სერვერის შეცდომა' 
        });
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
    console.log(`📊 Stats: http://localhost:${PORT}/stats`);
    console.log(`❤️  Health: http://localhost:${PORT}/health`);
});
