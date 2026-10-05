const http = require('http');
const crypto = require('crypto');
const fs = require('fs');
const path = require('path');
const historian = require('../historian-ai/grounding');

const PORT = Number(process.env.PORT || 8080);
const PUBLIC_ORIGIN = String(process.env.PUBLIC_ORIGIN || '').replace(/\/$/, '');
const SESSION_TTL_SECONDS = Math.min(Math.max(Number(process.env.SESSION_TTL_SECONDS || 900), 120), 3600);
const MAX_BODY_BYTES = 32 * 1024;
const sessions = new Map();
const historianAudit = [];
const publicDir = path.join(__dirname, 'public');

function token(bytes = 18) {
  return crypto.randomBytes(bytes).toString('base64url');
}

function now() {
  return Date.now();
}

function json(res, status, payload) {
  const body = JSON.stringify(payload);
  res.writeHead(status, {
    'Content-Type': 'application/json; charset=utf-8',
    'Content-Length': Buffer.byteLength(body),
    'Cache-Control': 'no-store',
    'Access-Control-Allow-Origin': '*',
    'Access-Control-Allow-Headers': 'Content-Type, X-Host-Token, X-Join-Token',
    'Access-Control-Allow-Methods': 'GET,POST,DELETE,OPTIONS'
  });
  res.end(body);
}

function error(res, status, message, code = 'invalid_request') {
  json(res, status, { ok: false, error: code, message });
}

function readBody(req) {
  return new Promise((resolve, reject) => {
    let total = 0;
    const chunks = [];
    req.on('data', chunk => {
      total += chunk.length;
      if (total > MAX_BODY_BYTES) {
        reject(new Error('body_too_large'));
        req.destroy();
        return;
      }
      chunks.push(chunk);
    });
    req.on('end', () => {
      if (chunks.length === 0) return resolve({});
      try { resolve(JSON.parse(Buffer.concat(chunks).toString('utf8').replace(/^\uFEFF/, ''))); }
      catch (_) { reject(new Error('invalid_json')); }
    });
    req.on('error', reject);
  });
}

function cleanText(value, max) {
  if (typeof value !== 'string') return '';
  return value.trim().slice(0, max);
}

function cleanOptions(value) {
  if (!Array.isArray(value)) return [];
  return value.slice(0, 3).map(item => cleanText(item, 160)).filter(Boolean);
}

function isExpired(session) {
  return !session || session.expiresAt <= now();
}

function findSession(id) {
  const session = sessions.get(id);
  if (isExpired(session)) {
    if (session) sessions.delete(id);
    return null;
  }
  return session;
}

function requireSession(req, res, id) {
  const session = findSession(id);
  if (!session) {
    error(res, 404, '会话不存在或已过期', 'session_expired');
    return null;
  }
  return session;
}

function requireHost(req, res, session, body = {}) {
  const supplied = cleanText(req.headers['x-host-token'] || body.hostToken, 160);
  if (!supplied || supplied !== session.hostToken) {
    error(res, 403, '主将凭证无效', 'host_forbidden');
    return false;
  }
  return true;
}

function requireGuest(req, res, session, body = {}) {
  const supplied = cleanText(req.headers['x-join-token'] || body.joinToken, 160);
  if (!supplied || supplied !== session.joinToken) {
    error(res, 403, '参谋邀请码无效', 'guest_forbidden');
    return false;
  }
  return true;
}

function publicSession(session, includeHost) {
  const result = {
    ok: true,
    sessionId: session.id,
    state: session.state,
    expiresAt: session.expiresAt,
    snapshot: session.snapshot,
    options: session.options,
    joinUrl: session.joinUrl,
    qrUrl: `/api/sessions/${encodeURIComponent(session.id)}/qr.png`,
    nextSuggestionId: session.suggestions.length + 1
  };
  if (includeHost) result.hostToken = session.hostToken;
  return result;
}

function cleanup() {
  for (const [id, session] of sessions) {
    if (session.expiresAt <= now()) sessions.delete(id);
  }
}

async function handle(req, res) {
  if (req.method === 'OPTIONS') return json(res, 204, {});
  const url = new URL(req.url, `http://${req.headers.host || 'localhost'}`);
  const parts = url.pathname.split('/').filter(Boolean);

  if (req.method === 'GET' && url.pathname === '/healthz')
    return json(res, 200, { ok: true, service: 'public-advisor', now: now() });

  if (req.method === 'GET' && parts[0] === 'join' && parts[1]) {
    const pagePath = path.join(publicDir, 'index.html');
    const html = fs.readFileSync(pagePath, 'utf8')
      .replace('__SESSION_ID__', JSON.stringify(parts[1]))
      .replace('__JOIN_TOKEN__', JSON.stringify(url.searchParams.get('token') || ''));
    res.writeHead(200, { 'Content-Type': 'text/html; charset=utf-8', 'Cache-Control': 'no-store' });
    return res.end(html);
  }

  if (req.method === 'GET' && parts[0] === 'api' && parts[1] === 'sessions' &&
      parts[3] === 'qr.png') {
    const session = requireSession(req, res, parts[2]);
    if (!session || !requireHost(req, res, session)) return;
    try {
      const QRCode = require('qrcode');
      const image = await QRCode.toBuffer(session.joinUrl, {
        type: 'png', width: 360, margin: 2, errorCorrectionLevel: 'M'
      });
      res.writeHead(200, {
        'Content-Type': 'image/png',
        'Content-Length': image.length,
        'Cache-Control': 'no-store'
      });
      return res.end(image);
    } catch (err) {
      return error(res, 503, '二维码组件尚未安装', 'qr_unavailable');
    }
  }

  if (req.method === 'POST' && parts[0] === 'api' && parts[1] === 'historian' && parts[2] === 'ask') {
    let body;
    try { body = await readBody(req); } catch (err) { return error(res, 400, '请求体不是有效 JSON', err.message); }
    const question = cleanText(body.question, 800);
    if (!question) return error(res, 400, '问题不能为空', 'question_required');
    const result = await historian.answerQuestion(question, cleanText(body.context, 2000));
    const audit = {
      auditId: token(8),
      createdAt: now(),
      question,
      retrieved: result.retrieved || [],
      mode: result.mode,
      answerVersion: 'grounding-v1'
    };
    historianAudit.push(audit);
    if (historianAudit.length > 100) historianAudit.shift();
    return json(res, 200, { ok: true, ...result, auditId: audit.auditId, answerVersion: audit.answerVersion });
  }

  if (parts[0] !== 'api' || parts[1] !== 'sessions')
    return error(res, 404, '接口不存在', 'not_found');

  if (req.method === 'POST' && parts.length === 2) {
    let body;
    try { body = await readBody(req); } catch (err) { return error(res, 400, '请求体不是有效 JSON', err.message); }
    const options = cleanOptions(body.options);
    if (options.length === 0) return error(res, 400, '至少需要一个可选方案', 'options_required');
    const id = token(8);
    const hostToken = token(24);
    const joinToken = token(18);
    const expiresAt = now() + SESSION_TTL_SECONDS * 1000;
    const joinUrl = `${PUBLIC_ORIGIN || `http://${req.headers.host || 'localhost'}`}/join/${id}?token=${encodeURIComponent(joinToken)}`;
    const session = {
      id, hostToken, joinToken, expiresAt, state: 'awaiting_guest',
      snapshot: {
        runId: cleanText(body.runId, 120),
        chapterIndex: Number.isInteger(body.chapterIndex) ? body.chapterIndex : -1,
        nodeId: Number.isInteger(body.nodeId) ? body.nodeId : 0,
        question: cleanText(body.question, 500),
        context: cleanText(body.context, 2000),
        resources: body.resources && typeof body.resources === 'object' ? body.resources : {}
      },
      options,
      suggestions: [],
      clientRequestIds: new Set(),
      joinUrl
    };
    sessions.set(id, session);
    return json(res, 201, publicSession(session, true));
  }

  if (parts.length < 3) return error(res, 404, '缺少会话编号', 'session_required');
  const session = requireSession(req, res, parts[2]);
  if (!session) return;

  let body = {};
  if (req.method === 'POST' || req.method === 'DELETE') {
    try { body = await readBody(req); } catch (err) { return error(res, 400, '请求体不是有效 JSON', err.message); }
  }

  if (req.method === 'GET' && parts.length === 3) {
    const joinToken = cleanText(url.searchParams.get('token'), 160);
    const isGuestRead = joinToken && joinToken === session.joinToken;
    if (!isGuestRead && !requireHost(req, res, session, body)) return;
    return json(res, 200, publicSession(session, false));
  }

  if (req.method === 'GET' && parts[3] === 'suggestions') {
    if (!requireHost(req, res, session, body)) return;
    const after = Math.max(0, Number(url.searchParams.get('after') || 0));
    return json(res, 200, {
      ok: true, state: session.state, expiresAt: session.expiresAt,
      suggestions: session.suggestions.filter(item => item.id > after)
    });
  }

  if (req.method === 'POST' && parts[3] === 'suggestions') {
    if (!requireGuest(req, res, session, body)) return;
    const requestId = cleanText(body.clientRequestId, 120);
    if (requestId && session.clientRequestIds.has(requestId)) {
      const existing = session.suggestions.find(item => item.clientRequestId === requestId);
      return json(res, 200, { ok: true, duplicate: true, suggestion: existing });
    }
    if (session.state !== 'awaiting_guest') return error(res, 409, '本局已经收到建议', 'suggestion_closed');
    const optionIndex = Number(body.optionIndex);
    const guestLabel = cleanText(body.guestLabel, 40);
    if (!guestLabel || !Number.isInteger(optionIndex) || optionIndex < 0 || optionIndex >= session.options.length)
      return error(res, 400, '昵称和选项不能为空', 'suggestion_invalid');
    const suggestion = {
      id: session.suggestions.length + 1,
      guestLabel,
      optionIndex,
      optionText: session.options[optionIndex],
      reason: cleanText(body.reason, 500),
      clientRequestId: requestId,
      createdAt: now()
    };
    session.suggestions.push(suggestion);
    if (requestId) session.clientRequestIds.add(requestId);
    session.state = 'suggestion_ready';
    return json(res, 201, { ok: true, suggestion });
  }

  if (req.method === 'POST' && parts[3] === 'decision') {
    if (!requireHost(req, res, session, body)) return;
    if (!session.suggestions.length) return error(res, 409, '尚未收到参谋建议', 'suggestion_missing');
    const accepted = Boolean(body.accepted);
    session.state = accepted ? 'accepted' : 'declined';
    session.decision = { accepted, suggestionId: session.suggestions[session.suggestions.length - 1].id, decidedAt: now() };
    return json(res, 200, { ok: true, state: session.state, decision: session.decision });
  }

  if (req.method === 'DELETE' && parts.length === 3) {
    if (!requireHost(req, res, session, body)) return;
    sessions.delete(session.id);
    return json(res, 200, { ok: true, state: 'closed' });
  }

  return error(res, 404, '接口不存在', 'not_found');
}

setInterval(cleanup, 30 * 1000).unref();
const server = http.createServer((req, res) => handle(req, res).catch(err => {
  console.error(err);
  error(res, 500, '服务暂时不可用', 'internal_error');
}));
server.listen(PORT, '0.0.0.0', () => console.log(`public-advisor listening on ${server.address().port}`));
