const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const root = path.resolve(__dirname, '../..');
const read = pathString => fs.readFileSync(path.join(root, pathString), 'utf8');
const ui = read('Assets/Scripts/UI/InviteCoCreationUI.cs');
const state = read('Assets/Scripts/UI/InviteSessionState.cs');
const tracker = read('Assets/Scripts/Managers/RunHistoryTracker.cs');
const history = read('Assets/Scripts/Managers/RunHistoryData.cs');
const saves = read('Assets/Scripts/Managers/LocalSaveManager.cs');
const copy = read('Documentation/文案组待改稿汇总.md');
let pass = 0;
function check(condition, message) {
  assert.ok(condition, message);
  console.log('PASS ' + message);
  pass++;
}
const region = (start, end) => {
  const first = ui.indexOf(start);
  const last = ui.indexOf(end, first + start.length);
  assert.ok(first >= 0 && last > first, 'Missing method anchors ' + start);
  return ui.slice(first, last);
};
const opening = region('public void OpenInvitePanel()', 'private void CloseInvitePanel()');
check(/if \(!inviteSession\.Matches\(snapshot\)\)/.test(opening),
  'Reopening reviewed invitation does not reset completed matching sessions');
check(!/Status == InviteSessionStatus\.Accepted \|\||Status == InviteSessionStatus\.Declined\)/.test(opening),
  'Accepted and declined statuses no longer force a new invitation');
check(/if \(inviteSession\.Status == InviteSessionStatus\.GuestSubmitted\)/.test(opening),
  'Only pending review opens the accept/decline panel');
const card = region('private void RenderInviteCard()', 'private void CopyBattleInvitation()');
check(/RenderAdvisorHistory\(\);/.test(card),
  'Every open refreshes persisted advisor history');
check(/if \(copyInvitationButton != null\) copyInvitationButton\.gameObject\.SetActive\(!completed\)/.test(card),
  'Completed invitation does not show stale copy-link button');
check(/if \(inviteAgainButton != null\) inviteAgainButton\.gameObject\.SetActive\(completed\)/.test(card),
  'Re-inviting is a separate explicit button');
check(/private void RestartInvitation\(\)/.test(card) &&
  /inviteSession\.Begin\(snapshot, GetCurrentOptionTexts\(\)\)/.test(card),
  'Re-invite starts a fresh session only after deliberate click');
const render = region('private void RenderAdvisorHistory()', 'private static string CompactAdvisorText(');
check(/RunHistoryTracker\.Instance/.test(render) && /tracker\.ExportHistory\(\)/.test(render) &&
  /history\.advisorEchoes/.test(render),
  'History is read from this run tracker, not volatile review UI or network');
check(/!string\.IsNullOrWhiteSpace\(history\.runId\)/.test(render),
  'History is not presented when there is no valid run identity');
check(/echoes\.Count - 1; i >= 0; i--/.test(render),
  'All records are shown, with latest suggestions first');
check(/echo\.accepted/.test(render) && /echo\.guestLabel/.test(render) &&
  /echo\.optionText/.test(render) && /echo\.reason/.test(render),
  'Each record includes review status, identity, option, and reason');
check(/advisorHistoryHeading\.gameObject\.SetActive\(hasRecords\)/.test(render) &&
  /advisorHistoryText\.gameObject\.SetActive\(hasRecords\)/.test(render),
  'Empty-run history section is hidden');
const build = region('private void CreateInviteOverlay(', 'private List<string> GetCurrentOptionTexts');
check(build.indexOf('InvitationActions') < build.indexOf('advisorHistoryHeading = CreateWrappedText') &&
  build.indexOf('advisorHistoryHeading = CreateWrappedText') < build.indexOf('CreateAdvisorPanels()'),
  'History is appended below invite actions inside the existing scroll content');
check(/inviteAgainButton\.onClick\.AddListener\(RestartInvitation\)/.test(build),
  'Reinvite control is connected to a real callback');
check(/current\.advisorEchoes\.Add\(new AdvisorEchoData/.test(tracker) &&
  /current\.advisorEchoes\.Count \+ 1/.test(tracker),
  'Distinct accepted/declined advisors stay recorded, including repeated attempts');
check(/history = RunHistoryTracker\.Instance/.test(saves) &&
  /RunHistoryTracker\.Instance\.RestoreHistory\(data\.history/.test(saves),
  'Save/load persists the history being displayed');
check(/advisorEchoes = new List<AdvisorEchoData>/.test(history) &&
  /current = new RunHistoryData\(\)/.test(tracker),
  'New runs get independent advisor history');
check(copy.includes('### 本局参谋意见（新增回看区）') &&
  copy.includes('server/public-advisor/public/index.html'),
  'Copywriting handoff includes the new UI and the external guest form');
console.log('advisor-history-regression: ' + pass + '/' + pass);
