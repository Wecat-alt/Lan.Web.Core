import('./vite.config.js')
  .then(()=>console.log('loaded config'))
  .catch(e=>{ console.error(e); process.exit(1); })
