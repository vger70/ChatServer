# ChatServer
 A very simple chat server

The ChatServer executable is in bin\Release directory. It runs on Microsoft Windows OS with .Net Framework version >= 4.5.2. It opens a console window and run chat server listening on tcp port 10000. When the telnet client connects, server assign a client id to the new client and run new thread th handle communications between client and server. Logging messages is minimal.
