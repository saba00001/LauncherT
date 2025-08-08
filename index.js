import express from 'express';
import mysql from 'mysql2/promise';
import crypto from 'crypto';

const app = express();
app.use(express.json());

const config = {
  launcher_key: 'GENESIS_LAUNCHER_2024_SECURE_KEY_CHANGE_THIS',
  current_version: '1.3.0',
  cloudflare_r2_url: 'https://pub-ec17f648d3ea4f508fabfdb6c1fc6518.r2.dev/bin.zip',
  database: {
    host: 'sql310.infinityfree.com',
    user: 'if0_39656209',
    password: '9k83Rgk3HIBQk10',
    database: 'if0_39656209_hora',
  },
  max_sessions_per_user: 1,
  maintenance_mode: false,
};

// DB კავშირი
let pool = mysql.createPool({
  host: config.database.host,
  user: config.database.user,
  password: config.database.password,
  database: config.database.database,
  waitForConnections: true,
  connectionLimit: 10,
  queueLimit: 0,
});

// დამოწმება უსაფრთხო შედარებით
function verifyLauncherKey(providedKey) {
  if (!providedKey) return false;
  return crypto.timingSafeEqual(Buffer.from(config.launcher_key), Buffer.from(providedKey));
}

// IP აღება Express-ში
function getClientIP(req) {
  return (
    req.headers['cf-connecting-ip'] ||
    req.headers['x-forwarded-for']?.split(',')[0]?.trim() ||
    req.socket.remoteAddress ||
    'unknown'
  );
}

// API Endpoint: /status
app.get(['/','/status'], async (req, res) => {
  if (config.maintenance_mode) {
    return res.status(503).json({
      error: 'Server maintenance',
      message: 'სერვერი ტექნიკურ მომსახურებაშია. სცადეთ მოგვიანებით.',
      retry_after: 3600
    });
  }
  
  try {
    const [rows] = await pool.query("SELECT COUNT(*) AS count FROM active_sessions WHERE created_at > DATE_SUB(NOW(), INTERVAL 1 HOUR)");
    const activePlayers = rows[0].count || 0;
    
    return res.json({
      status: 'online',
      version: config.current_version,
      database: 'connected',
      active_players: activePlayers,
      timestamp: Date.now(),
      server_time: new Date().toISOString()
    });
  } catch (e) {
    console.error('Status check failed:', e);
    return res.status(500).json({status: 'error', message: 'Internal server error'});
  }
});

// API Endpoint: /version
app.get('/version', (req, res) => {
  res.json({
    version: config.current_version,
    download_url: config.cloudflare_r2_url,
    changelog: {
      '1.3.0': 'უსაფრთხოების გაუმჯობესება, ახალი ფუნქციები',
      '1.2.0': 'ბუგების გასწორება, გამოსახულების გაუმჯობესება',
      '1.1.0': 'ანტი-ჩიტ სისტემის დამატება'
    }
  });
});

// API Endpoint: /download
app.get('/download', (req, res) => {
  const launcherKey = req.header('X-Launcher-Key');
  if (!verifyLauncherKey(launcherKey)) {
    return res.status(401).json({error: 'Invalid launcher key'});
  }
  res.redirect(config.cloudflare_r2_url);
});

// API Endpoint: /auth
app.post('/auth', async (req, res) => {
  const launcherKey = req.header('X-Launcher-Key');
  if (!verifyLauncherKey(launcherKey)) {
    return res.status(401).json({error: 'Invalid launcher key'});
  }
  
  const { nickname, launcher_key: clientLauncherKey, version, hardware_id: hwid } = req.body;
  
  if (!nickname || typeof nickname !== 'string' || nickname.length < 3 || nickname.length > 24) {
    return res.status(400).json({error: 'Nickname must be 3-24 characters'});
  }
  if (!/^[a-zA-Z0-9_\[\]]+$/.test(nickname)) {
    return res.status(400).json({error: 'Invalid nickname characters. Only letters, numbers, _ [ ] allowed'});
  }
  if (version !== config.current_version) {
    return res.status(409).json({error: 'Version mismatch', required_version: config.current_version, your_version: version});
  }
  
  try {
    const connection = await pool.getConnection();
    const ip = getClientIP(req);
    
    // გადამოწმება აქტიური სესიების
    const [activeSessionsRows] = await connection.query(
      "SELECT COUNT(*) AS count FROM active_sessions WHERE nickname = ? AND created_at > DATE_SUB(NOW(), INTERVAL 2 HOUR)",
      [nickname]
    );
    if (activeSessionsRows[0].count >= config.max_sessions_per_user) {
      connection.release();
      return res.status(409).json({error: 'Already connected. Please wait 2 hours or contact admin.'});
    }
    
    // ძველი სესიების წაშლა
    await connection.query("DELETE FROM active_sessions WHERE created_at < DATE_SUB(NOW(), INTERVAL 6 HOUR)");
    
    // მოთამაშის დამატება ან განახლება
    await connection.query(`
      INSERT INTO players (nickname, launcher_key, last_login, ip_address, hardware_id)
      VALUES (?, ?, NOW(), ?, ?)
      ON DUPLICATE KEY UPDATE last_login = NOW(), ip_address = ?, hardware_id = ?
    `, [nickname, clientLauncherKey, ip, hwid, ip, hwid]);
    
    // სესიის გენერაცია
    const sessionToken = crypto.randomBytes(32).toString('hex');
    
    await connection.query(`
      INSERT INTO active_sessions (nickname, session_token, created_at, ip_address)
      VALUES (?, ?, NOW(), ?)
      ON DUPLICATE KEY UPDATE session_token = ?, created_at = NOW(), ip_address = ?
    `, [nickname, sessionToken, ip, sessionToken, ip]);
    
    connection.release();
    
    return res.json({
      success: true,
      session_token: sessionToken,
      message: 'Authorization successful',
      server_time: new Date().toISOString()
    });
    
  } catch (e) {
    console.error('Auth error:', e);
    return res.status(500).json({error: 'Authentication service unavailable'});
  }
});

// სხვა Endpoints ასევე შეიძლება მსგავსად გადმოვიტანოთ

// სერვერი 3000 პორტზე
app.listen(3000, () => {
  console.log('Genesis Launcher API is running on port 3000');
});
