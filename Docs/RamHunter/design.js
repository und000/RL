'use strict';
const W=10,H=7;
const baseChips=[
 {id:'A',name:'검격 칩',effect:'기본 공격 보강',x:2,y:2,shape:[[0,0],[1,0],[0,1]]},
 {id:'B',name:'기동 칩',effect:'이동·회피 보강',x:4,y:2,shape:[[0,0],[1,0],[2,0],[1,1]]},
 {id:'C',name:'방어 칩',effect:'생존 능력 보강',x:8,y:4,shape:[[0,0],[0,1],[1,1]]},
 {id:'D',name:'기술 칩',effect:'파츠 기술 보강',x:7,y:1,shape:[[0,0],[0,1],[0,2]]}
];
let chips=[],powers=[],selected='P1';
const board=document.getElementById('board');
function cells(item){return item.shape.map(([x,y])=>[x+item.x,y+item.y]);}
function allItems(){return [...chips,...powers];}
function touching(a,b){return cells(a).some(([x,y])=>cells(b).some(([u,v])=>Math.abs(x-u)+Math.abs(y-v)===1));}
function getLevels(){const levels=Object.fromEntries(chips.map(c=>[c.id,0]));for(const p of powers){const seen=new Set(),queue=chips.filter(c=>touching(p,c));while(queue.length){const c=queue.shift();if(seen.has(c.id))continue;seen.add(c.id);levels[c.id]++;for(const other of chips)if(!seen.has(other.id)&&touching(c,other))queue.push(other);}}return levels;}
function valid(item){const occupied=new Set(allItems().filter(i=>i.id!==item.id).flatMap(cells).map(([x,y])=>x+','+y));return cells(item).every(([x,y])=>x>=0&&x<W&&y>=0&&y<H&&!occupied.has(x+','+y));}
function current(){return allItems().find(i=>i.id===selected);}
function draw(message){const levels=getLevels(),occupancy=new Map();allItems().forEach(i=>cells(i).forEach(([x,y])=>occupancy.set(x+','+y,i)));board.replaceChildren();
 for(let y=0;y<H;y++)for(let x=0;x<W;x++){
  const item=occupancy.get(x+','+y),button=document.createElement('button');button.type='button';button.className='cell';button.dataset.x=x;button.dataset.y=y;
  if(item){const isPower=item.id.startsWith('P'),level=levels[item.id]||0;button.classList.add(isPower?'source':'chip');if(level)button.classList.add('enhanced');if(item.id===selected)button.classList.add('selected');button.setAttribute('aria-pressed',String(item.id===selected));button.setAttribute('aria-label',`${y+1}행 ${x+1}열 ${item.name}${isPower?'':level?' 기본 활성, 강화 +'+level:' 기본 활성, 추가 강화 없음'}`);
   const labelCell=cells(item)[0];
   if(labelCell[0]===x&&labelCell[1]===y){button.append(document.createTextNode(isPower?'⚡':item.id));if(!isPower){const em=document.createElement('em');em.textContent=level?'+'+level:'기본';button.append(em);}}
  }else button.setAttribute('aria-label',`${y+1}행 ${x+1}열 빈칸`);
  button.addEventListener('click',()=>{if(item){selected=item.id;draw(`${item.name} 선택. 빈칸을 눌러 이동하세요.`);board.querySelector(`[data-x="${x}"][data-y="${y}"]`).focus();return;}const active=current();if(!active)return;const candidate={...active,x,y};if(!valid(candidate)){draw('다른 칩과 겹치거나 인벤토리 밖으로 나갑니다. 다른 위치를 선택하세요.');return;}active.x=x;active.y=y;clearPreset();draw(`${active.name} 이동 완료. 연결 상태를 다시 계산했습니다.`);board.querySelector(`[data-x="${x}"][data-y="${y}"]`).focus();});board.append(button);
 }
 document.getElementById('chip-results').innerHTML=chips.map(c=>`<div class="result" data-chip="${c.id}" data-level="${levels[c.id]}"><div><b>${c.id} · ${c.name}</b><small>${c.effect} · 기본 활성</small></div><span class="${levels[c.id]?'level':'base-level'}">${levels[c.id]?'+'+levels[c.id]:'기본'}</span></div>`).join('');
 const active=current();document.getElementById('selection').textContent=active?`선택: ${active.name} — 빈칸을 눌러 이동${selected.startsWith('P')?'':', 회전 버튼으로 모양 변경'}`:'칩이나 전원을 선택하세요.';document.getElementById('rotate-chip').disabled=!active||selected.startsWith('P');document.getElementById('board-status').textContent=message||'모든 칩이 기본 효과를 제공합니다. 전원과 연결하면 추가 강화됩니다.';
}
function clearPreset(){document.querySelectorAll('[data-preset]').forEach(b=>b.classList.remove('selected'));}
function preset(name){chips=baseChips.map(c=>({...c,shape:c.shape.map(p=>[...p])}));powers=[{id:'P1',name:'전원 1',x:name==='loose'?0:1,y:name==='loose'?0:2,shape:[[0,0]]}];if(name==='bridge'||name==='two')chips.find(c=>c.id==='D').y=2;if(name==='two')powers.push({id:'P2',name:'전원 2',x:9,y:4,shape:[[0,0]]});selected='P1';clearPreset();document.querySelector(`[data-preset="${name}"]`).classList.add('selected');const messages={loose:'전원과 떨어져 있어도 모든 칩의 기본 효과는 활성화됩니다.',one:'A·B·D는 전원과 연결되어 +1. 떨어진 C도 기본 효과는 유지됩니다.',bridge:'D를 아래로 옮겨 C까지 연결했습니다. 이제 모든 칩이 +1입니다.',two:'서로 다른 전원 둘이 같은 칩 묶음에 연결되어 모두 +2입니다.'};draw(messages[name]);}
document.querySelectorAll('[data-preset]').forEach(b=>b.addEventListener('click',()=>preset(b.dataset.preset)));
document.getElementById('reset-board').addEventListener('click',()=>preset('loose'));
document.getElementById('rotate-chip').addEventListener('click',()=>{const active=current();if(!active||selected.startsWith('P'))return;const raw=active.shape.map(([x,y])=>[-y,x]),minX=Math.min(...raw.map(p=>p[0])),minY=Math.min(...raw.map(p=>p[1]));const shape=raw.map(([x,y])=>[x-minX,y-minY]);if(!valid({...active,shape})){draw('현재 위치에서는 회전할 공간이 부족합니다. 빈 공간으로 옮겨 회전하세요.');return;}active.shape=shape;clearPreset();draw(`${active.name} 회전 완료. 기본 효과는 유지됩니다.`);});
document.getElementById('add-power').addEventListener('click',()=>{const item={id:'P'+(powers.length+1),name:'전원 '+(powers.length+1),x:0,y:0,shape:[[0,0]]};for(let y=0;y<H;y++)for(let x=0;x<W;x++){item.x=x;item.y=y;if(valid(item)){powers.push(item);selected=item.id;clearPreset();draw('새 전원을 선택했습니다. 원하는 칩 옆의 빈칸을 누르세요.');return;}}draw('전원을 놓을 빈칸이 없습니다. 공간을 확보하세요.');});
preset('loose');
const light=document.getElementById('lightbox');document.querySelectorAll('[data-lightbox]').forEach(b=>b.addEventListener('click',()=>{const img=b.querySelector('img');document.getElementById('light-image').src=b.dataset.lightbox;document.getElementById('light-image').alt=img.alt;document.getElementById('light-caption').textContent=img.alt;light.showModal();}));document.getElementById('close-lightbox').addEventListener('click',()=>light.close());light.addEventListener('click',e=>{if(e.target===light)light.close();});document.getElementById('print-page').addEventListener('click',()=>window.print());
const links=[...document.querySelectorAll('nav a')];const observer=new IntersectionObserver(entries=>entries.forEach(e=>{if(e.isIntersecting)links.forEach(a=>a.classList.toggle('active',a.hash==='#'+e.target.id));}),{rootMargin:'-10% 0px -65% 0px'});document.querySelectorAll('header[id],section[id]').forEach(s=>observer.observe(s));
