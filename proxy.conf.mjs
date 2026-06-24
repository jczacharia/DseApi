export default {
  '/api': {
    target: (process.env['ASPNETCORE_URLS'] && process.env['ASPNETCORE_URLS'].split(';')[0]) || 'http://localhost:5038',
    secure: false,
    changeOrigin: true,
    pathRewrite: {
      '^/api': '/api',
    },
  },
};
