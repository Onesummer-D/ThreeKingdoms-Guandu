// Diagnostic only: calculate loudness/duration from the *original* clips.
// Does not resample or normalize source audio; listening checks remain essential.
const fs=require('node:fs');
const path=require('node:path');
const cp=require('node:child_process');
const root=path.resolve(__dirname,'../..');
const targets=[
 'Assets/Audio/output_sfx/箭矢破空音.ogg',
 'Assets/Audio/output_sfx/拔剑_.ogg',
 'Assets/Audio/output_sfx/锯子锯木头的声音.ogg',
 'Assets/Audio/output_sfx/园艺-用铲子挖树叶和碎片_.ogg',
 'Assets/Audio/output_sfx/纸张翻动音效.ogg',
 'Assets/Audio/output_sfx/警报声.ogg',
 'Assets/Audio/output_sfx/达成成就.ogg',
 'Assets/Audio/output_sfx/打造成功.ogg',
 'Assets/Audio/output_sfx/拼图成功音效.ogg',
 'Assets/Audio/output_sfx/移动一格音.ogg',
 'Assets/Audio/output_sfx/走格子走一格.ogg',
 'Assets/Audio/output_sfx/数值变化.ogg',
 'Assets/Audio/output_sfx/数值变化音.ogg',
 'Assets/Audio/output_sfx/错误.ogg',
 'Assets/Audio/output_sfx/鼠标点击音.ogg',
 'Assets/Audio/output_bgm/奔跑的骏马.ogg',
 'Assets/Audio/output_bgm/火烧乌巢.ogg'
];
function ff(program,args) {
 const result=cp.spawnSync(program,args,{encoding:'utf8',timeout:30000,maxBuffer:1024*1024});
 if(result.error)throw result.error;
 if(result.status!==0)throw Error(program+' failed: '+result.stderr.slice(-700));
 return result;
}
try {
 ff('ffmpeg',['-version']);
 ff('ffprobe',['-version']);
} catch(error) {
 console.log('AUDIO ANALYSIS SKIPPED (ffmpeg/ffprobe unavailable): '+error.message);
 process.exit(0);
}
const n=value=>Number.isFinite(+value)?+value:null;
const fmt=value=>value===null?'unknown':value.toFixed(1);
console.log('Clip | Seconds | Mean dBFS | Peak dBFS | Leading silence (> -45dB) | Flags');
for (const asset of targets) {
 const local=path.join(root,asset);
 if(!fs.existsSync(local))throw Error('Missing asset: '+asset);
 const probe=ff('ffprobe',['-v','error','-show_entries','format=duration','-of','default=noprint_wrappers=1:nokey=1',local]);
 const duration=n(probe.stdout.trim());
 const level=ff('ffmpeg',['-hide_banner','-nostats','-v','info','-i',local,'-vn','-sn','-dn','-af','volumedetect','-f','null','-']);
 const mean=n(level.stderr.match(/mean_volume:\s*(-?[\d.]+) dB/)?.[1]);
 const peak=n(level.stderr.match(/max_volume:\s*(-?[\d.]+) dB/)?.[1]);
 const silence=ff('ffmpeg',['-hide_banner','-nostats','-v','info','-i',local,'-vn','-sn','-dn','-af','silencedetect=noise=-45dB:d=0.08','-f','null','-']);
 const begins=silence.stderr.match(/silence_start:\s*0(?:\.0+)?\b/);
 const silenceEnd=begins ? n(silence.stderr.match(/silence_end:\s*([\d.]+)/)?.[1]) : 0;
 const flags=[];
 if(mean!==null&&mean<-28) flags.push('QUIET_SOURCE');
 if(peak!==null&&peak< -15) flags.push('LOW_PEAK');
 if(peak!==null&&peak> -0.2) flags.push('NEAR_FULL_SCALE');
 if(silenceEnd!==null&&silenceEnd>0.2) flags.push('SLOW_ONSET');
 if(duration!==null&&duration>8&&asset.includes('output_sfx')) flags.push('LONG_SFX');
 console.log(asset.split('/').pop()+' | '+fmt(duration)+' | '+fmt(mean)+
   ' | '+fmt(peak)+' | '+fmt(silenceEnd)+' | '+(flags.join(',')||'—'));
}
console.log('These are file-level dBFS values (not Unity playback loudness or perceptual LUFS); do not apply automatic gain from this report.');
