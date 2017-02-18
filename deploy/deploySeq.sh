echo "---------------------------------Deployment Starting ------------------------------------------------------"
date
cd ~/PowerNetwork
git checkout .; 
# this is using ssh key of user 
# username: deploy_readonly_user, password : Deploy-1232
git pull origin release 
sudo systemctl stop kestrel-powernetwork-ui.service
cd src/PowerNetwork.Core
dotnet restore
dotnet build
cd ../PowerNetwork.Web
dotnet restore
dotnet build
dotnet publish --configuration=debug --output=release
sudo systemctl start kestrel-powernetwork-ui.service
sudo systemctl status kestrel-powernetwork-ui.service
echo "---------------------------------Deployment End ------------------------------------------------------"

