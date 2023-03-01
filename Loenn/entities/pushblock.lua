local drawableSprite = require("structs.drawable_sprite")
local utils = require("utils")
local pushblock = {}

pushblock.name = "canyon/pushblock"
pushblock.depth = -9999
pushblock.placements = 
{
    name = "PushBlock",
    data = {
        ["stickyTop"] = false,
        ["stickyBottom"] = false,
        ["stickyLeft"] = false,
        ["stickyRight"] = false,
        ["isTemple"] = false
    }
}

local blockTexture = "objects/canyon/pushblock/idle"
local blockTempleTexture = "objects/canyon/pushblock/idleTemple"
local gooTexture = "objects/canyon/pushblock/stickyGoo00"

function pushblock.sprite(room, entity)
    local sprites = {}

    if entity.isTemple then
        local blockSprite = drawableSprite.fromTexture(blockTempleTexture, entity)
        table.insert(sprites, blockSprite)
    else
        local blockSprite = drawableSprite.fromTexture(blockTexture, entity)
        table.insert(sprites, blockSprite)
    end

    if entity.stickyTop then
        local gooSprite = drawableSprite.fromTexture(gooTexture, entity)
        gooSprite:setJustification(0.5, 0.5)
        gooSprite.rotation = 0
        table.insert(sprites, gooSprite)
    end
    if entity.stickyBottom then
        local gooSprite = drawableSprite.fromTexture(gooTexture, entity)
        gooSprite:setJustification(0.5, 0.5)
        gooSprite.rotation = math.pi
        table.insert(sprites, gooSprite)
    end
    if entity.stickyLeft then
        local gooSprite = drawableSprite.fromTexture(gooTexture, entity)
        gooSprite:setJustification(0.5, 0.5)
        gooSprite.rotation = -math.pi / 2
        table.insert(sprites, gooSprite)
    end
    if entity.stickyRight then
        local gooSprite = drawableSprite.fromTexture(gooTexture, entity)
        gooSprite:setJustification(0.5, 0.5)
        gooSprite.rotation = math.pi / 2
        table.insert(sprites, gooSprite)
    end

    return sprites
end

return pushblock