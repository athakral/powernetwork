cd ~/PowerNetwork
git checkout .; 
git pull origin master
cd src/PowerNetwork.Web
sudo systemctl stop kestrel-powernetwork-ui.service
dotnet publish --configuration release
sudo systemctl start kestrel-powernetwork-ui.service
