local drawableSprite = require("structs.drawable_sprite")
local drawableNinePatch = require("structs.drawable_nine_patch")
local utils = require("utils")
local toggleblock = {}

toggleblock.name = "canyon/toggleblock"
toggleblock.depth = -9999
toggleblock.nodeLimits = {1, -1}
toggleblock.minimumSize = {16, 16}
toggleblock.canResize = {true, true}
toggleblock.nodeLineRenderType = "line"
toggleblock.placements = 
{
    name = "ToggleBlock",
    placementType = "rectangle",
    data = {
        width = 16,
        height = 16,
        ["travelSpeed"] = 5.0,
        ["oscillate"] = false,
        ["stopAtEnd"] = false,
        ["customTexturePath"] = ""
    }
}

local frameNinePatchOptions = {
    mode = "fill",
    borderMode = "repeat"
}

local frameTexture = "objects/canyon/toggleblock/block1"
local midTexture = "objects/canyon/toggleblock/middleRed00"

function toggleblock.sprite(room, entity)
    local sprites = {}
    local x, y = entity.x or 0, entity.y or 0
    local width, height = entity.width or 16, entity.height or 16

    local frameNinePatch = drawableNinePatch.fromTexture(frameTexture, frameNinePatchOptions, x, y, width, height)
    local frameSprites = frameNinePatch:getDrawableSprite()
    local midSprite = drawableSprite.fromTexture(midTexture, entity)
    midSprite:addPosition(math.floor(width / 2), math.floor(height / 2))
    midSprite.depth = -10000

    table.insert(sprites, midSprite)

    for i, sprite in ipairs(frameSprites) do
        table.insert(sprites, frameSprites[i])
    end

    return sprites
end

return toggleblock