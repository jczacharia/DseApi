// Dev proxy. The `/api/sources/{key}/search` route is pointed straight at the local
// Elasticsearch instance, emulating the backend's SourceSearchEndpoint (which POSTs the
// request body to `/{source-{key}-search}/_search`). This lets the UI be built against real
// _search responses before the .NET backend is running. The aggregate `pnc` source fans out
// across every `source-*-search` index — exactly how the PNC home page searches everything.
const ELASTICSEARCH = 'http://localhost:45705';
const ES_AUTH = 'Basic ' + Buffer.from('elastic:elasticsearch').toString('base64');

module.exports = {
  '/api/sources': {
    target: ELASTICSEARCH,
    secure: false,
    changeOrigin: true,
    headers: {Authorization: ES_AUTH},
    // Object form (regex + $1 backref) — survives the dev-server's config clone, unlike a function.
    // `pnc` fans out across every source index; any other key maps to its own read alias.
    // Rules apply in order: the pnc rule rewrites first, so the generic rule no longer matches it.
    pathRewrite: {
      '^/api/sources/pnc/search': '/source-*-search/_search',
      '^/api/sources/([^/]+)/search': '/source-$1-search/_search',
    },
  },
  '/api': {
    target: process.env['ASPNETCORE_URLS']?.split(';')[0] || 'http://localhost:5130',
    secure: false,
    changeOrigin: true,
    pathRewrite: {
      '^/api': '/api',
    },
  },
};
