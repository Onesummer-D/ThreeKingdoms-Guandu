// Only player-visible, already-recorded gameplay can be used for a run explanation.
// This is game telemetry, never evidence for a historical claim.
const clampText = (value, limit = 130) =>
  String(value == null ? '' : value).replace(/[\r\n\t]+/g, ' ').replace(/[<>]/g, '').trim().slice(0, limit);
const number = value => typeof value === 'number' && Number.isFinite(value)
  ? Math.max(-100000, Math.min(100000, value)) : null;
const snapshot = value => {
  if (!value || typeof value !== 'object') return null;
  const fields = ['troop', 'food', 'strategy', 'risk'];
  if (!fields.every(key => number(value[key]) !== null)) return null;
  return Object.fromEntries(fields.map(key => [key, number(value[key])]));
};
function sanitizeRun(value) {
  if (!value || typeof value !== 'object' || Array.isArray(value)) return null;
  const decisions = Array.isArray(value.decisions) ? value.decisions.slice(-40) : [];
  const games = Array.isArray(value.miniGames) ? value.miniGames.slice(-12) : [];
  const ending = value.completed === true && value.ending && typeof value.ending === 'object'
    ? {
      name: clampText(value.ending.name, 40),
      reason: clampText(value.ending.reason, 150),
      id: clampText(value.ending.id, 24),
      keyDecisions: Array.isArray(value.ending.keyDecisions)
        ? value.ending.keyDecisions.filter(n => Number.isSafeInteger(n) && n > 0).slice(0, 10) : []
    } : null;
  return {
    completed: value.completed === true,
    integrityValid: value.integrityValid !== false,
    start: snapshot(value.start),
    current: snapshot(value.current),
    ending,
    decisions: decisions.filter(item => item && typeof item === 'object' &&
      Number.isSafeInteger(item.sequence) && item.sequence > 0).map(item => ({
      sequence: item.sequence,
      chapterIndex: Number.isInteger(item.chapterIndex) ? Math.max(-1, Math.min(9, item.chapterIndex)) : -1,
      optionText: clampText(item.optionText, 110),
      troopEffect: number(item.troopEffect),
      foodEffect: number(item.foodEffect),
      strategyEffect: number(item.strategyEffect),
      riskEffect: number(item.riskEffect)
    })),
    miniGames: games.filter(item => item && typeof item === 'object').map(item => ({
      gameType: clampText(item.gameType, 35),
      success: item.success === true,
      failedAttempts: Number.isSafeInteger(item.failedAttempts)
        ? Math.max(0, Math.min(10000, item.failedAttempts)) : 0
    }))
  };
}
const format = n => n === null ? '未知' : String(Math.round(n * 10) / 10);
function formatResources(data) {
  if (!data) return '暂无完整资源快照';
  return '兵力' + format(data.troop) + '、粮草' + format(data.food) +
    '、计策' + format(data.strategy) + '、风险' + format(data.risk);
}
function formatEffects(item) {
  const fields = [['兵力', item.troopEffect], ['粮草', item.foodEffect],
    ['计策', item.strategyEffect], ['风险', item.riskEffect]];
  const changed = fields.filter(pair => pair[1] !== null && pair[1] !== 0)
    .map(pair => pair[0] + (pair[1] > 0 ? '+' : '') + format(pair[1]));
  return changed.length ? '；选项配置变化：' + changed.join('、') : '';
}
function isRunQuestion(question) {
  return /(本局|这局|这一局|我的(选择|决策|兵力|粮草|计策|风险|战绩|资源|结局)|我(为什么|怎么)(会)?(输|赢|失败|获胜)|我(输了|赢了|失败了|获胜了)|哪一步|复盘|刚才的选择|为什么(会)?(输|赢|失败|获胜)|怎么(会)?(输|赢|失败|获胜))/.test(String(question || ''));
}
function answerRunQuestion(run, question) {
  if (!run) return {
    answer: '我尚未收到这局实际发生的决策记录，不能凭历史故事推断你为什么输或赢。请在进入游戏并作出选择后再问我。',
    mode: 'run_unavailable', citations: [], retrieved: []
  };
  if (!run.integrityValid) return {
    answer: '这局的记录未通过完整性检查，我暂时不能可靠地据此判断胜负原因。你仍可查看战绩报告中的原始记录。',
    mode: 'run_unavailable', citations: [], retrieved: []
  };
  const parts = [];
  if (run.completed && run.ending && run.ending.name) {
    parts.push('这局抵达了「' + run.ending.name + '」。' +
      (run.ending.reason ? '游戏记录的结局评价是「' + run.ending.reason + '」。' : '') + '[G01]');
  } else {
    parts.push('这局尚无已记录的终局，因此现在不能断言输赢原因。以下只复盘已经发生的选择。[G01]');
  }
  const key = run.ending && run.ending.keyDecisions.length
    ? run.decisions.filter(d => run.ending.keyDecisions.includes(d.sequence))
    : run.decisions.slice(-3);
  const selected = key.length ? key.slice(-3) : run.decisions.slice(-3);
  if (selected.length) parts.push('已记录的' + (run.completed ? '关键' : '最近') + '选择：' +
    selected.map(d => '第' + (d.chapterIndex < 0 ? '?' : (d.chapterIndex + 1)) +
      '幕「' + (d.optionText || '未命名选项') + '」' + formatEffects(d)).join('；') + '。[G01]');
  if (run.current) parts.push('当前' + (run.completed ? '终局' : '已发生') +
    '资源为' + formatResources(run.current) + '。[G01]');
  if (run.start && run.current) {
    const changed = Object.keys(run.current).some(k => run.start[k] !== run.current[k]);
    if (!changed) parts.push('记录中的资源暂未观察到变化。[G01]');
  }
  if (/(小游戏|拼图|地道|滑块)/.test(question) && run.miniGames.length) {
    parts.push('小游戏记录：' + run.miniGames.slice(-3).map(g =>
      (g.gameType || '小游戏') + (g.success ? '成功' : '未成功') +
      '（失败尝试' + g.failedAttempts + '次）').join('、') + '。[G01]');
  }
  parts.push('这是游戏事件与结局评价的回顾，不是史料中的真实战局。记录能说明你做了什么以及游戏如何评价，却不能单凭数值证明某一步是唯一原因。[G01]');
  return {
    answer: parts.join('\n\n'),
    mode: 'run_evidence', retrieved: ['G01'],
    citations: [{ id: 'G01', title: '本局已发生的战况记录', source: '游戏内 RunHistoryTracker；非历史史料' }]
  };
}
module.exports = { sanitizeRun, answerRunQuestion, isRunQuestion, formatResources };
