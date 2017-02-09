echo "---------------------------------Deployment Starting ------------------------------------------------------"
date
cd ~/PowerNetwork
git checkout .; 
# this is using ssh key of user 
# username: deploy_readonly_user, password : Deploy-1232
git pull origin release 
cd src/PowerNetwork.Web
sudo systemctl stop kestrel-powernetwork-ui.service
dotnet publish --configuration release
sudo systemctl start kestrel-powernetwork-ui.service

echo "---------------------------------Deployment End ------------------------------------------------------"

