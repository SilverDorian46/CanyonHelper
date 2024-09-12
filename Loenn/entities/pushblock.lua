local drawableSprite = require("structs.drawable_sprite")
local fakeTilesHelper = require("helpers.fake_tiles")
local utils = require("utils")

local pushblock = {}

pushblock.name = "canyon/pushblock"
pushblock.depth = -9999
pushblock.fieldInformation = fakeTilesHelper.getFieldInformation("customDebrisFromTileset")
pushblock.fieldOrder = {
    "x", "y",
    "customBlockTexture", "customGooTexture",
    "customDebrisFromTileset", "overrideDebris", "isTemple",
    "stickyTop", "stickyBottom", "stickyLeft", "stickyRight",
    "legacy"
}
pushblock.placements = 
{
    name = "PushBlock",
    data = {
        ["stickyTop"] = false,
        ["stickyBottom"] = false,
        ["stickyLeft"] = false,
        ["stickyRight"] = false,
        ["isTemple"] = false,
        legacy = false,
        customBlockTexture = "objects/canyon/pushblock/idle",
        customGooTexture = "objects/canyon/pushblock/stickyGoo",
        overrideDebris = false,
        customDebrisFromTileset = "5"
    }
}

local function getBlockTexture(entity)
    local data = entity.customBlockTexture
    local block = (data == nil or data == "") and "objects/canyon/pushblock/idle" or data
    
    return block
end

local function getGooTexture(entity)
    local data = entity.customGooTexture
    local goo = (data == nil or data == "") and "objects/canyon/pushblock/stickyGoo00" or data .. "00"
    
    return goo
end

function pushblock.sprite(room, entity)
    local sprites = {}

    if entity.isTemple then
        local blockSprite = drawableSprite.fromTexture(getBlockTexture(entity) .. "Temple", entity)
        table.insert(sprites, blockSprite)
    else
        local blockSprite = drawableSprite.fromTexture(getBlockTexture(entity), entity)
        table.insert(sprites, blockSprite)
    end

    if entity.stickyTop then
        local gooSprite = drawableSprite.fromTexture(getGooTexture(entity), entity)
        gooSprite:setJustification(0.5, 0.5)
        gooSprite.rotation = 0
        table.insert(sprites, gooSprite)
    end
    if entity.stickyBottom then
        local gooSprite = drawableSprite.fromTexture(getGooTexture(entity), entity)
        gooSprite:setJustification(0.5, 0.5)
        gooSprite.rotation = math.pi
        table.insert(sprites, gooSprite)
    end
    if entity.stickyLeft then
        local gooSprite = drawableSprite.fromTexture(getGooTexture(entity), entity)
        gooSprite:setJustification(0.5, 0.5)
        gooSprite.rotation = -math.pi / 2
        table.insert(sprites, gooSprite)
    end
    if entity.stickyRight then
        local gooSprite = drawableSprite.fromTexture(getGooTexture(entity), entity)
        gooSprite:setJustification(0.5, 0.5)
        gooSprite.rotation = math.pi / 2
        table.insert(sprites, gooSprite)
    end

    return sprites
end

return pushblock