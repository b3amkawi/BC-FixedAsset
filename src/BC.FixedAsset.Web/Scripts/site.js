(function(){
  var menu=document.querySelector('[data-menu]'),sidebar=document.getElementById('sidebar');
  if(menu&&sidebar)menu.addEventListener('click',function(){sidebar.classList.toggle('open')});
  document.querySelectorAll('.nav-item').forEach(function(link){var a=(link.getAttribute('href')||'').toLowerCase(),p=location.pathname.toLowerCase();if(a&&p===a)link.classList.add('active')});
  var language=document.getElementById('languageButton'),lang=localStorage.getItem('bc-language')||'th';
  function setLanguage(value){lang=value;document.documentElement.lang=value;if(language){var label=language.querySelector('span');if(label)label.textContent=value==='th'?'EN':'ไทย'}document.querySelectorAll('[data-th][data-en]').forEach(function(el){el.textContent=value==='th'?el.getAttribute('data-th'):el.getAttribute('data-en')})}
  if(language)language.addEventListener('click',function(){setLanguage(lang==='th'?'en':'th');localStorage.setItem('bc-language',lang)});setLanguage(lang);
  var form=document.getElementById('form1'),loading=document.getElementById('loadingOverlay');
  document.querySelectorAll('[data-detail-trigger]').forEach(function(row){
    function openDetail(){var trigger=document.getElementById(row.getAttribute('data-detail-trigger'));if(trigger)trigger.click()}
    row.addEventListener('click',function(e){if(e.target.closest('a,button,input,select,textarea,label'))return;openDetail()});
    row.addEventListener('keydown',function(e){if(e.target!==row)return;if(e.key==='Enter'||e.key===' '){e.preventDefault();openDetail()}});
  });
  if(form&&loading)form.addEventListener('submit',function(e){var t=e.submitter;if(t&&(t.classList.contains('icon-button')||(t.id||'').indexOf('btnClose')>=0))return;loading.classList.add('show')});
  window.addEventListener('pageshow',function(){if(loading)loading.classList.remove('show')});
})();
