//Import some assets from Vortex we'll need.
const path = require('path');
const { fs, log, util } = require('vortex-api');

function main(context) {
    //This is the main function Vortex will run when detecting the game extension. 

    context.registerGame({
        id: GAME_ID,
        name: "Traveller's Rest",
        mergeMods: true,
        queryPath: findGame,
        supportedTools: [],
        queryModPath: () => 'Windows/BepInEx/plugins',
        logo: 'gameart.jpg',
        executable: () => 'Windows/TravellersRest.exe',
        requiredFiles: [
            'Windows/TravellersRest.exe',
            'Windows/BepInEx/core/BepInEx.dll'
        ],
        // setup: prepareForModding,
        setup: () => {},
        environment: {
            SteamAPPId: STEAMAPP_ID,
        },
        details: {
            steamAppId: STEAMAPP_ID,
            gogAppId: GOGAPP_ID,
        },
    });
    return true
}

module.exports = {
    default: main,
};

// Nexus Mods domain for the game. e.g. nexusmods.com/travellersrest
const GAME_ID = 'travellersrest';

//Steam Application ID, you can get this from https://steamdb.info/apps/
const STEAMAPP_ID = '1139980';

//GOG Application ID, you can get this from https://www.gogdb.org/
const GOGAPP_ID = '1353960921';

function findGame() {
    return util.GameStoreHelper.findByAppId([STEAMAPP_ID, GOGAPP_ID])
        .then(game => game.gamePath);
}