declare global { interface Window { F1_RUNTIME_CONFIG?: {apiBaseUrl?:string}; } }

/** Public origin only; Firebase user tokens are still supplied by each authenticated request. */
export function apiUrl(path:string):string {
  const base=window.F1_RUNTIME_CONFIG?.apiBaseUrl??'';
  if(!path.startsWith('/'))throw new Error('API paths must be absolute paths');
  if(!base)return path;
  const origin=new URL(base);
  if(origin.origin!==base||origin.username||origin.password||!['https:','http:'].includes(origin.protocol)
    ||origin.protocol==='http:'&&!['localhost','127.0.0.1'].includes(origin.hostname))throw new Error('API base must be an HTTPS origin');
  return base+path;
}
