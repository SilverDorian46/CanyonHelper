local utils = require("utils")
local spinorb = {}

spinorb.name = "canyon/spinorb"
spinorb.depth = -9999
spinorb.texture = "objects/canyon/spinorb/idle00"
spinorb.justification = {0.5, 0.5}
spinorb.placements = 
{
    name = "SpinOrb"
}

function spinorb.rectangle(room, entity)
    local x, y = entity.x or 0, entity.y or 0
    return utils.rectangle(x - 9, y - 21, 17, 30)
end

return spinorb