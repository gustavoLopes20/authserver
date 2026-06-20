$ CREATE DATABASE db\_identityServer\_prd;
$ CREATE USER 'usrIdentityServer'@'%' IDENTIFIED BY '5Z7A$c#y$9Dg';
$ GRANT ALL PRIVILEGES ON db\_identityServer\_prd.\* TO 'usrIdentityServer'@'%';
$ FLUSH PRIVILEGES;



$ systemctl enable authServer.service
$ systemctl start authServer.service
$ systemctl restart authServer.service
$ service nginx restart

location /identity/ {
proxy\_pass http://localhost:37740/;
proxy\_set\_header   X-Real-IP $remote\_addr;
proxy\_set\_header   Host      $http\_host;  
}

\[Unit]
Description=.NET Identity Serve running
\[Service]
WorkingDirectory=/var/www/authServer
ExecStart=/usr/bin/dotnet /var/www/authServer/AuthServer.dll
Restart=always

# Restart service after 10 seconds if the dotnet service crashes:

RestartSec=10
KillSignal=SIGINT
SyslogIdentifier=dotnet-example
User=www-data
Environment=ASPNETCORE\_ENVIRONMENT=Production
Environment=DOTNET\_PRINT\_TELEMETRY\_MESSAGE=false
Environment=ASPNETCORE\_URLS=http://127.0.0.1:37740
tandardOutput=/var/log/authServer-output.log
StandardError=/var/log/authServer-error.log

\[Install]
WantedBy=multi-user.target



## Exportar certificado

$ openssl pkcs12 -export -out /var/www/authServer/Cert/certificado.pfx   
-inkey /etc/letsencrypt/archive/rentainvestsistema.com.br/privkey9.pem   
-in /etc/letsencrypt/archive/rentainvestsistema.com.br/fullchain9.pem   
-certfile /etc/letsencrypt/archive/rentainvestsistema.com.br/fullchain9.pem

senha=U430@k6'fh;d
U430@k6'fh;d



U430@k6\\u0027fh;d

