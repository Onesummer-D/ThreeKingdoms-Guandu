const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const root = path.resolve(__dirname, '../..');
const read = p => fs.readFileSync(path.join(root,p),'utf8');
const ui = read('Assets/Scripts/UI/BattleReportUI.cs');
const analyzer = read('Assets/Scripts/UI/BattleReportAnalyzer.cs');
const doc = read('Documentation/文案组待改稿汇总.md');
let n=0;
function check(ok,msg){assert.ok(ok,msg);console.log('PASS '+msg);n++;}
function method(start,end){
  const a=ui.indexOf(start); const b=ui.indexOf(end,a+start.length);
  assert.ok(a>=0&&b>a,'Missing source anchors for '+start);
  return ui.slice(a,b);
}
const tendency=method('private float CreateTendencyCard(', 'private float CreateTrendCard(');
check(/"本局决策倾向 · " \+ tendency\.label,\s*40f,\s*TextAlignmentOptions\.Center/.test(tendency),
  'Decision tendency heading horizontally centered');
check(/CreateText\(card\.transform, tendency\.summary,\s*29f,\s*TextAlignmentOptions\.Center/.test(tendency),
  'War style description centered in expanded report card');
check(!tendency.includes('"谋议方式 · " + tendency.consultationLabel') &&
      tendency.includes('"谋议方式：" + tendency.consultationLabel') &&
      tendency.includes('CreateEvidenceDetailOverlay'),
  'Counsel name appears in detail overlay, not on the poster');
check(/CreateText\(card\.transform, tendency\.consultationSummary,\s*29f/.test(tendency),
  'Counsel summary and war-style summary use identical font size');
const trend=method('private float CreateTrendCard(', 'private void CreateTrendDetailOverlay(');
check(/CreateText\(card\.transform, "四项资源趋势", 40f,\s*TextAlignmentOptions\.Center/.test(trend),
  'Resource heading uses short copy and centered alignment');
check(!trend.includes('真实节点记录'),'Unwanted subtitle removed');
check(trend.includes('CreateResourceSummary(')&&trend.includes('CreateButton(card.transform, "查看详情"'),
  'Resource graph and details control still present');
const popup=method('private void CreateTrendDetailOverlay(', 'private void OpenTrendDetail(');
check(/CreateText\(panel\.transform, "资源趋势详情", 46f/.test(popup),
  'Trend detail modal uses requested short title');
check(!popup.includes('决策前后节点'),'Old modal subtitle removed');
check(popup.includes('CreateResourceSummary(')&&popup.includes('TrendDetailPlot'),
  'Full detailed resource chart unchanged');
const culture=method('private float CreateCultureCard(', 'private float CreateAdvisorEchoCard(');
check(/"史官简注", 38f,\s*TextAlignmentOptions\.Center/.test(culture),
  'Historian short note heading centered');
check(culture.includes('GetCultureNote(campaignMap.GetReachedEndingNodeId())'),
  'Historian note continues to show existing content');
const advisor=method('private float CreateAdvisorEchoCard(', 'private IEnumerator CapturePoster(');
check(/"共谋回声", 38f,\s*TextAlignmentOptions\.Center/.test(advisor),
  'Co-planning echo heading centered');
check(advisor.includes('history.advisorEchoes')&&advisor.includes('主将未采纳'),
  'Co-planning actual advisor records preserved');
const exportMethod=method('private Texture2D RenderCompleteReport(', 'private static string GetPosterDirectory(');
check(exportMethod.includes('Instantiate(content, canvasRect, false)'),
  'Export uses exactly the same report content and inherits new titles');
check(['稳慎','奇谋','果决','兼采并用'].every(label=>analyzer.includes('"'+label+'"')),
  'Strategy labels retained with independent counsel dimension');
check(analyzer.includes('DecisionBehaviorCatalog.TryFind') &&
      !analyzer.includes('TendencyOrder ='),
  'Analyzer uses stable semantic choice IDs, not old keyword classifier');
check(analyzer.includes('label = "尚待观察"'), 'Unclassified save fallback retained');
check(doc.includes('| 纳谏践行 |') && doc.includes('| 审议自决 |') &&
      doc.includes('| 兼采并用 |'),
  'Copywriter document describes the actual dual-dimensional classifier');
check(doc.includes('- 四项资源趋势')&&doc.includes('- 资源趋势详情')&&
  !doc.includes('四项资源趋势 · 真实节点记录')&&
  !doc.includes('资源趋势详情 · 决策前后节点'),
  'Copywriter headline list synced to runtime UI');
console.log('battle-report-heading-regression: '+n+'/'+n);
