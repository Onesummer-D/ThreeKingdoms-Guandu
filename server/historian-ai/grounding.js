const fs = require('fs');
const path = require('path');

const corpus = JSON.parse(fs.readFileSync(path.join(__dirname, 'corpus.json'), 'utf8'));

function tokens(value) {
  return String(value || '').toLowerCase().match(/[\u4e00-\u9fff]|[a-z0-9]+/g) || [];
}

function retrieve(question, limit = 4) {
  const query = tokens(question);
  const scored = corpus.map(doc => {
    const haystack = tokens([doc.title, doc.source, doc.locator, doc.tags.join(' '), doc.summary, doc.boundary].join(' '));
    let score = 0;
    for (const token of query) {
      if (haystack.includes(token)) score += doc.tags.includes(token) ? 4 : 1;
    }
    return { doc, score };
  }).sort((a, b) => b.score - a.score || a.doc.id.localeCompare(b.doc.id));
  return scored.slice(0, limit).map(item => Object.assign({ score: item.score }, item.doc));
}

function buildGroundedPrompt(question, context, docs) {
  const evidence = docs.map(doc =>
    `[${doc.id}] ${doc.title}\n来源：${doc.source}\n${doc.summary}\n边界：${doc.boundary}`
  ).join('\n\n');
  return `你是“官渡之战”项目的受约束史料讲解员。只能使用下面的证据回答，不能补写未给出的日期、数字、原文或人物动机。每个事实段末尾必须带一个或多个证据编号，例如 [H04]。如果证据不足，直接说“现有卡片不足以确认”，并说明还缺什么。要明确区分史实、后世材料和游戏演绎。\n\n玩家问题：${question}\n\n当前游戏上下文：${context || '无'}\n\n证据卡片：\n${evidence}`;
}

function validateAnswer(answer, docs) {
  const text = String(answer || '').trim();
  const allowed = new Set(docs.map(doc => doc.id));
  const citations = [...text.matchAll(/\[(H\d{2})\]/g)].map(match => match[1]);
  const unknown = citations.filter(id => !allowed.has(id));
  if (!text || unknown.length || (text.length > 40 && citations.length === 0)) {
    return { valid: false, reason: unknown.length ? 'unknown_citation' : 'citation_required' };
  }
  return { valid: true, reason: '' };
}

function fallbackAnswer(question, docs) {
  if (!docs.length || docs[0].score === 0) {
    return '现有史料卡片不足以确认这个问题。请换一个与官渡、乌巢、粮道、人物或本局复盘相关的问题。';
  }
  return docs.slice(0, 4).map(doc =>
    `${doc.title}：${doc.summary}（${doc.boundary}）[${doc.id}]`
  ).join('\n');
}

async function askOpenAI(prompt) {
  const key = process.env.OPENAI_API_KEY;
  if (!key) return null;
  const endpoint = process.env.OPENAI_API_URL || 'https://api.openai.com/v1/chat/completions';
  const model = process.env.OPENAI_MODEL || 'gpt-5.4-mini';
  const response = await fetch(endpoint, {
    method: 'POST',
    headers: { 'Authorization': `Bearer ${key}`, 'Content-Type': 'application/json' },
    body: JSON.stringify({
      model,
      temperature: 0.1,
      max_completion_tokens: 500,
      messages: [
        { role: 'system', content: 'You are a grounded Chinese historian assistant. Follow the supplied evidence and citation rules exactly.' },
        { role: 'user', content: prompt }
      ]
    })
  });
  if (!response.ok) throw new Error(`model_http_${response.status}`);
  const data = await response.json();
  return data && data.choices && data.choices[0] && data.choices[0].message
    ? data.choices[0].message.content : null;
}

async function answerQuestion(question, context) {
  if (/(精确|准确|每一天|分钟|伤亡|逐日)/.test(String(question || ''))) {
    return {
      answer: '现有史料卡片不足以确认这个问题；卡片没有逐日精确伤亡或分钟级行军数据。',
      mode: 'grounded_refusal',
      citations: [],
      retrieved: []
    };
  }
  const docs = retrieve(question, 4);
  const prompt = buildGroundedPrompt(question, context, docs);
  let answer = null;
  let mode = 'grounded_fallback';
  try {
    answer = await askOpenAI(prompt);
    if (answer) mode = 'grounded_model';
  } catch (_) {
    answer = null;
  }
  const check = validateAnswer(answer, docs);
  if (!check.valid) {
    answer = fallbackAnswer(question, docs);
    mode = 'grounded_fallback';
  }
  return { answer, mode, citations: docs.map(doc => ({ id: doc.id, title: doc.title, source: doc.source })), retrieved: docs.map(doc => doc.id) };
}

module.exports = { corpus, retrieve, buildGroundedPrompt, validateAnswer, fallbackAnswer, answerQuestion };
