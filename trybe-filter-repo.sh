
### GIT FILTER-REPO ###

## NÃO EXECUTE ESSE SCRIPT DIRETAMENTE
## Esse script foi feito para uso do
## script 'trybe-publisher' fornecido 
## pela Trybe. 

[[ $# == 1 ]] && \
[[ $1 == "trybe-security-parameter" ]] && \
git filter-repo \
    --path .trybe \
    --path .github \
    --path trybe.yml \
    --path trybe-filter-repo.sh \
    --path src/TryBets.Test.Test/ContextBetsTest.cs \
    --path src/TryBets.Test.Test/ContextMatchesTest.cs \
    --path src/TryBets.Test.Test/ContextOddsTest.cs \
    --path src/TryBets.Test.Test/ContextUsersTest.cs \
    --path src/TryBets.Test.Test/OddServiceTest.cs \
    --path src/TryBets.Test.Test/TestReq01.cs \
    --path src/TryBets.Test.Test/TestReq02.cs \
    --path src/TryBets.Test.Test/TestReq03.cs \
    --path src/TryBets.Test.Test/TestReq04.cs \
    --path src/TryBets.Test.Test/TestReq05.cs \
    --path src/TryBets.Test.Test/docker-log-bets.txt \
    --path src/TryBets.Test.Test/docker-log-matches.txt \
    --path src/TryBets.Test.Test/docker-log-odds.txt \
    --path src/TryBets.Test.Test/docker-log-users.txt \
    --path src/TryBets.Test.Test/docker-test-bets.sh \
    --path src/TryBets.Test.Test/docker-test-matches.sh \
    --path src/TryBets.Test.Test/docker-test-odds.sh \
    --path src/TryBets.Test.Test/docker-test-users.sh \
    --path src/TryBets.Test.Test/Usings.cs \
    --path src/TryBets.Test.Test/TryBets.Test.Test.csproj \
    --path src/TryBets.Test.Test/TryBets.Test.Test.sln \
    --path src/TryBets.Test.Test \
    --path img/der.png \
    --path docker-compose.yml \
    --path README.md \
    --path .gitignore \
    --invert-paths --force