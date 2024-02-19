#!/bin/bash
echo "iniciando dockerfile matches"
echo "Dockerfile script executed from: ${PWD}"

rm docker-log-matches.txt

echo "Construindo imagem"
docker build --tag trybets:matches ../TryBets.Matches/

echo "Construindo container"
docker run --name trybebets_matches -d trybets:matches

echo "Aguardando pelo container"
sleep 20

echo "Checando logs"
docker logs trybebets_matches &> docker-log-matches.txt

echo "Removendo container"
docker rm -f trybebets_matches

echo "Removendo imagem"
docker image rm --force trybets:matches