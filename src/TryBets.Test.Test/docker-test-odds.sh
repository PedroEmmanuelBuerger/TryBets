#!/bin/bash
echo "iniciando dockerfile odds"
echo "Dockerfile script executed from: ${PWD}"

rm docker-log-odds.txt

echo "Construindo imagem"
docker build --tag trybets:odds ../TryBets.Odds/

echo "Construindo container"
docker run --name trybebets_odds -d trybets:odds

echo "Aguardando pelo container"
sleep 20

echo "Checando logs"
docker logs trybebets_odds &> docker-log-odds.txt

echo "Removendo container"
docker rm -f trybebets_odds

echo "Removendo imagem"
docker image rm --force trybets:odds