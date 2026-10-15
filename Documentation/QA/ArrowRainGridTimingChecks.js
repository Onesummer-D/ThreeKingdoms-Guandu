const assert=require('node:assert/strict');
const fs=require('node:fs');
const path=require('node:path');
const root=path.join(__dirname,'../..');
const read=p=>fs.readFileSync(path.join(root,p),'utf8');
const audio=read('Assets/AudioManager.cs');
const dialogue=read('Assets/Scripts/Managers/DialogueSystem.cs');
const grid=read('Assets/Scripts/MiniGames/GridGame/GridGameManager.cs');
const player=read('Assets/Scripts/MiniGames/GridGame/GridPlayer.cs');
const ui=read('Assets/Scripts/Managers/FinalUIManager.cs');
let passed=0;
function check(value,description){assert.ok(value,description);passed++;console.log('PASS '+description);}
const rain=audio.match(/private IEnumerator ArrowRainRoutine\(AudioClip clip\)([\s\S]*?)\n    public void StopArrowRain/);
check(!!rain,'Dedicated arrow rain coroutine exists');
const delays=[...rain[1].matchAll(/([0-9]\.[0-9]+)f/g)].map(x=>+x[1]);
check(delays.length>=7 && new Set(delays).size>=4,'Volley has at least eight staggered, non-uniform attacks');
check(/sfxSource\.PlayOneShot\(clip/.test(rain[1]) && /WaitForSecondsRealtime\(intervals\[i\]\)/.test(rain[1]),
  'Arrow clips are layered directly rather than suppressed by SFX repeat cooldown');
check(/Time\.unscaledTime \+ volleyDuration/.test(audio)&&/effectDuck = Mathf\.Min\(effectDuck, 0\.20f\)/.test(audio),
  'Background music remains ducked throughout the volley');
check(/public void StopArrowRain\(\)/.test(audio) && /StopCoroutine\(arrowRainCoroutine\)/.test(audio),
  'Arrow rain can be cancelled when moving to another node');
check(/if \(nodeId == 100104 \|\| nodeId == 500416\)\s*AudioManager\.Instance\.PlayArrowRain/.test(dialogue),
  'Opening and IF6 arrow rain use multi-shot cue');
check(/AudioManager\.Instance\?\.StopArrowRain\(\)/.test(dialogue),
  'Dialogue transitions stop queued arrows');
check(/public float PlayGridResultSFX\(bool success\)/.test(audio) &&
  /success \? achievementSFX : alertSFX/.test(audio) &&
  /result\.length \+ 0\.3f/.test(audio),
  'Grid success/failure wait derives from actual sound duration');
check(/void GameOver\(bool isWin\)([\s\S]*?)\n    IEnumerator ReturnToPlot/.test(grid),
  'Single grid finish handler exists');
const ending=grid.match(/void GameOver\(bool isWin\)([\s\S]*?)\n    IEnumerator ReturnToPlot/)[1];
check(/if \(!isGameActive\) return/.test(ending) && /isGameActive = false/.test(ending),
  'Ending sound cannot trigger twice');
check(/player\.FreezeAtResult\(\)/.test(ending) &&
  /tile\.EnableClick\(false\)/.test(ending) &&
  /confirmButton\.gameObject\.SetActive\(false\)/.test(ending),
  'Grid becomes read-only after success or failure');
check(/PlayGridResultSFX\(isWin\)/.test(ending) &&
  /StartCoroutine\(ReturnToPlot\(isWin, holdSeconds\)\)/.test(ending),
  'Sound is started before transition timer');
const transition=grid.match(/IEnumerator ReturnToPlot\(bool isWin, float holdSeconds\)([\s\S]*?)\n    public bool IsGameActive/);
check(!!transition && /WaitForSeconds\(Mathf\.Max\(0\.9f, holdSeconds\)\)/.test(transition[1]),
  'Transition waits for entire cue and obeys game pause');
check(transition[1].indexOf('WaitForSeconds')<transition[1].indexOf('gridGamePanel.SetActive(false)') &&
  transition[1].indexOf('gridGamePanel.SetActive(false)')<transition[1].indexOf('callback?.Invoke(isWin)'),
  'Result screen stays until after wait, and callback occurs last');
check(/public void FreezeAtResult\(\)/.test(player)&&/transform\.position = targetPosition/.test(player)&&
  /isActive = false/.test(player),'Player remains on destination square and ignores WASD');
const onMove=grid.match(/public void OnPlayerMove\(int row, int col\)([\s\S]*?)\n    public void OnPlayerReachEnd/)[1];
check(!/PlayAlert\(/.test(onMove),'Fail alert is not fired a second time on enemy contact');
const onFinish=ui.match(/void OnGridGameFinished\(bool success, int nextNodeId\)([\s\S]*?)\n    bool IsEndingNode/)[1];
check(!/PlayAchievement\(/.test(onFinish) && !/PlayAlert\(/.test(onFinish),
  'Plot callback no longer plays outcome sound after the result screen is gone');
check(/OnMiniGameFinished\(success, targetNodeId\)/.test(onFinish) &&
  /ModifyTroop\(-20\)/.test(onFinish),'Existing branch and fail penalty remain unchanged');
console.log('arrow-rain-grid-timing: '+passed+'/'+passed);
