#!/bin/bash
echo "iniciando dockerfile bets"
echo "Dockerfile script executed from: ${PWD}"

rm docker-log-bets.txt

echo "Construindo imagem"
docker build --tag trybets:bets ../TryBets.Bets/

echo "Construindo container"
docker run --name trybebets_bets -d trybets:bets

echo "Aguardando pelo container"
sleep 20

echo "Checando logs"
docker logs trybebets_bets &> docker-log-bets.txt

echo "Removendo container"
docker rm -f trybebets_bets

echo "Removendo imagem"
docker image rm --force trybets:bets