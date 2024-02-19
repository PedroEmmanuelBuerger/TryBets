#!/bin/bash
echo "iniciando dockerfile users"
echo "Dockerfile script executed from: ${PWD}"

rm docker-log-users.txt

echo "Construindo imagem"
docker build --tag trybets:users ../TryBets.Users/

echo "Construindo container"
docker run --name trybebets_users -d trybets:users

echo "Aguardando pelo container"
sleep 20

echo "Checando logs"
docker logs trybebets_users &> docker-log-users.txt

echo "Removendo container"
docker rm -f trybebets_users

echo "Removendo imagem"
docker image rm --force trybets:users