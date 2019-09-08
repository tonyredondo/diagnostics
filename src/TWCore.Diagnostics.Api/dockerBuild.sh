echo "Remove previous build"
rm -r app

echo "Creating folder structure..."
mkdir app

echo "Publishing project..."
dotnet clean -c Release -r linux-x64
dotnet build -c Release -r linux-x64
dotnet publish -c Release -r linux-x64 -v q -o ./app/

echo "Building docker image"
sudo docker build -t tonyredondo/diagnostics:3.0.0-beta .
sudo docker push tonyredondo/diagnostics:3.0.0-beta

echo "Remove build artifacts"
rm -r app
