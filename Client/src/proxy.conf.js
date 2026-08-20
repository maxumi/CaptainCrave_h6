// src/proxy.conf.js

const env = process.env;

const target =
  env["services__api__https__0"] ??
  env["services__api__http__0"];

module.exports = {
  "/api/**": {
    target,
    secure: false,
    changeOrigin: true
  },

"/uploads": {
    target,
    secure: false,
    changeOrigin: true
},

  "/hubs/**": {
    target,
    secure: false,
    changeOrigin: true,
    ws: true
  }
};