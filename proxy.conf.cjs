// Routes proxied from the Angular dev server (ng serve, :4200) back to the ASP.NET Core API.
// Under Aspire (Dse.AppHost), the API URL arrives as `services__api__{https,http}__0`.
// Falls back to ASPNETCORE_URLS / the http launch profile when ng serve runs standalone.
const target =
  process.env['services__api__https__0'] ||
  process.env['services__api__http__0'] ||
  (process.env['ASPNETCORE_URLS'] && process.env['ASPNETCORE_URLS'].split(';')[0]) ||
  'http://localhost:5038';

module.exports = [
  {
    context: ['/weatherforecast', '/api', '/openapi', '/scalar', '/health', '/alive'],
    target,
    secure: false,
    changeOrigin: true,
  },
];
