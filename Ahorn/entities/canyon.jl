module Canyon

using ..Ahorn, Maple

@mapdef Entity "canyon/spinorb" SpinOrb(x::Integer, y::Integer)
@mapdef Entity "canyon/pushblock" PushBlock(x::Integer, y::Integer)
@mapdef Entity "canyon/toggleblock" ToggleBlock(x1::Integer, y1::Integer, x2::Integer=x1+16, y2::Integer=y1, width::Integer=16, height::Integer=16)
@mapdef Entity "canyon/grannytemple" GrannyTemple(x::Integer, y::Integer)
@mapdef Entity "canyon/grannymonster" GrannyMonster(x::Integer, y::Integer)
@mapdef Entity "canyon/grannystart" GrannyStart(x::Integer, y::Integer)
@mapdef Entity "canyon/unbreakablestone" UnbreakableStone(x::Integer, y::Integer)

function toggleBlockFinalizer(entity)
    x, y = Ahorn.position(entity)

    width = Int(get(entity.data, "width", 8))
    height = Int(get(entity.data, "height", 8))

    entity.data["nodes"] = Tuple{Int, Int}[(x + width, y)]
end

const placements = Ahorn.PlacementDict(
    "Spin Orb (Canyon)" => Ahorn.EntityPlacement(
        SpinOrb
    ),
    "Push Block (Canyon)" => Ahorn.EntityPlacement(
        PushBlock,
        "point",
        Dict{String, Any}(
            "stickyTop" => false,
            "stickyBottom" => false,
            "stickyLeft" => false,
            "stickyRight" => false,
            "isTemple" => false
        )
    ),
    "Toggle Block (Canyon)" => Ahorn.EntityPlacement(
        ToggleBlock,
        "rectangle",
        Dict{String, Any}(
            "travelSpeed" => 5.0,
            "oscillate" => false,
            "stopAtEnd" => false,
            "customTexturePath" => ""
        ),
        toggleBlockFinalizer
    ),
    "Granny (Temple Entrance) (Canyon)" => Ahorn.EntityPlacement(
        GrannyTemple
    ),
    "Granny (Monster Showdown) (Canyon)" => Ahorn.EntityPlacement(
        GrannyMonster
    ),
    "Granny (Intro Cutscene) (Canyon)" => Ahorn.EntityPlacement(
        GrannyStart
    ),
    "Unbreakable Stone (Canyon)" => Ahorn.EntityPlacement(
        UnbreakableStone
    )
)

Ahorn.nodeLimits(entity::ToggleBlock) = 1, -1
Ahorn.minimumSize(entity::ToggleBlock) = 16, 16
Ahorn.resizable(entity::ToggleBlock) = true, true

function Ahorn.selection(entity::ToggleBlock)
    nodes = get(entity.data, "nodes", ())
    x, y = Ahorn.position(entity)

    width = Int(get(entity.data, "width", 32))
    height = Int(get(entity.data, "height", 32))

    res = Ahorn.Rectangle[Ahorn.Rectangle(x, y, width, height)]
    
    for node in nodes
        nx, ny = Int.(node)

        push!(res, Ahorn.Rectangle(nx, ny, width, height))
    end

    return res
end

frame = "objects/canyon/toggleblock/block1"
midResource = "objects/canyon/toggleblock/middleRed00"

function renderSingleToggleBlock(ctx::Ahorn.Cairo.CairoContext, x::Number, y::Number, width::Number, height::Number)
    midSprite = Ahorn.getSprite(midResource, "Gameplay")
    
    tilesWidth = div(width, 8)
    tilesHeight = div(height, 8)

    for i in 2:tilesWidth - 1
        Ahorn.drawImage(ctx, frame, x + (i - 1) * 8, y, 8, 0, 8, 8)
        Ahorn.drawImage(ctx, frame, x + (i - 1) * 8, y + height - 8, 8, 16, 8, 8)
    end

    for i in 2:tilesHeight - 1
        Ahorn.drawImage(ctx, frame, x, y + (i - 1) * 8, 0, 8, 8, 8)
        Ahorn.drawImage(ctx, frame, x + width - 8, y + (i - 1) * 8, 16, 8, 8, 8)
    end

    for i in 2:tilesWidth - 1, j in 2:tilesHeight - 1
        Ahorn.drawImage(ctx, frame, x + (i - 1) * 8, y + (j - 1) * 8, 8, 8, 8, 8)
    end

    Ahorn.drawImage(ctx, frame, x, y, 0, 0, 8, 8)
    Ahorn.drawImage(ctx, frame, x + width - 8, y, 16, 0, 8, 8)
    Ahorn.drawImage(ctx, frame, x, y + height - 8, 0, 16, 8, 8)
    Ahorn.drawImage(ctx, frame, x + width - 8, y + height - 8, 16, 16, 8, 8)

    Ahorn.drawImage(ctx, midSprite, x + div(width - midSprite.width, 2), y + div(height - midSprite.height, 2))
end

function renderToggleBlock(ctx::Ahorn.Cairo.CairoContext, width::Number, height::Number, entity::ToggleBlock)
    midSprite = Ahorn.getSprite(midResource, "Gameplay")
    
    nodes = get(entity.data, "nodes", ())

    tilesWidth = div(width, 8)
    tilesHeight = div(height, 8)

    for node in nodes
        nx, ny = Int.(node)

        for i in 2:tilesWidth - 1
            Ahorn.drawImage(ctx, frame, nx + (i - 1) * 8, ny, 8, 0, 8, 8)
            Ahorn.drawImage(ctx, frame, nx + (i - 1) * 8, ny + height - 8, 8, 16, 8, 8)
        end
    
        for i in 2:tilesHeight - 1
            Ahorn.drawImage(ctx, frame, nx, ny + (i - 1) * 8, 0, 8, 8, 8)
            Ahorn.drawImage(ctx, frame, nx + width - 8, ny + (i - 1) * 8, 16, 8, 8, 8)
        end
    
        for i in 2:tilesWidth - 1, j in 2:tilesHeight - 1
            Ahorn.drawImage(ctx, frame, nx + (i - 1) * 8, ny + (j - 1) * 8, 8, 8, 8, 8)
        end
    
        Ahorn.drawImage(ctx, frame, nx, ny, 0, 0, 8, 8)
        Ahorn.drawImage(ctx, frame, nx + width - 8, ny, 16, 0, 8, 8)
        Ahorn.drawImage(ctx, frame, nx, ny + height - 8, 0, 16, 8, 8)
        Ahorn.drawImage(ctx, frame, nx + width - 8, ny + height - 8, 16, 16, 8, 8)
    
        Ahorn.drawImage(ctx, midSprite, nx + div(width - midSprite.width, 2), ny + div(height - midSprite.height, 2))
    end
end

function Ahorn.renderSelectedAbs(ctx::Ahorn.Cairo.CairoContext, entity::ToggleBlock, room::Maple.Room)
    sprite = get(entity.data, "sprite", "block")

    width = Int(get(entity.data, "width", 32))
    height = Int(get(entity.data, "height", 32))

    px, py = Ahorn.position(entity)

    for node in get(entity.data, "nodes", ())
        nx, ny = Int.(node)

        theta = atan(py - ny, px - nx)
        Ahorn.drawArrow(ctx, px + width / 2, py + height / 2, nx + width / 2 + cos(theta) * 8, ny + height / 2 + sin(theta) * 8, Ahorn.colors.selection_selected_fc, headLength=6)

        px, py = nx, ny
    end

    renderToggleBlock(ctx, width, height, entity)
end

function Ahorn.renderAbs(ctx::Ahorn.Cairo.CairoContext, entity::ToggleBlock, room::Maple.Room)
    customTexture = get(entity.data, "customTexturePath", "")
    if (length(customTexture) > 0 && !occursin('"', customTexture))
        global frame = customTexture
    else
        global frame = "objects/canyon/toggleblock/block1"
    end

    sprite = get(entity.data, "sprite", "block")

    startX, startY = Int(entity.data["x"]), Int(entity.data["y"])

    width = Int(get(entity.data, "width", 32))
    height = Int(get(entity.data, "height", 32))

    px, py = Ahorn.position(entity)

    for node in get(entity.data, "nodes", ())
        nx, ny = Int.(node)

        theta = atan(py - ny, px - nx)
        Ahorn.drawArrow(ctx, px + width / 2, py + height / 2, nx + width / 2 + cos(theta) * 8, ny + height / 2 + sin(theta) * 8, Ahorn.colors.selection_selected_fc, headLength=6)

        px, py = nx, ny
    end

    renderSingleToggleBlock(ctx, startX, startY, width, height)
    renderToggleBlock(ctx, width, height, entity)
end

function Ahorn.selection(entity::SpinOrb)
    x, y = Ahorn.position(entity)

    return Ahorn.Rectangle(x - 8, y - 8, 16, 16)
end

function Ahorn.selection(entity::PushBlock)
    x, y = Ahorn.position(entity)

    return Ahorn.getSpriteRectangle("objects/canyon/pushblock/idle.png", x, y, jx=0.5, jy=0.5)
end

listOfTextures = ["objects/canyon/pushblock/stickyGoo00", "objects/canyon/pushblock/stickyGoo01", "objects/canyon/pushblock/stickyGoo02", "objects/canyon/pushblock/stickyGoo03"]

topTexture = rand(listOfTextures)
bottomTexture = rand(listOfTextures)
leftTexture = rand(listOfTextures)
rightTexture = rand(listOfTextures)

Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::SpinOrb) = Ahorn.drawSprite(ctx, "objects/canyon/spinorb/idle00.png", 0, 0, jx=0.5, jy=0.5)

function Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::PushBlock)
    if (get(entity.data, "isTemple", false))
        Ahorn.drawSprite(ctx, "objects/canyon/pushblock/idleTemple.png", 0, 0, jx=0.5, jy=0.5)
    else
        Ahorn.drawSprite(ctx, "objects/canyon/pushblock/idle.png", 0, 0, jx=0.5, jy=0.5)
    end
    stickyTop = get(entity.data, "stickyTop", false)
    stickyBottom = get(entity.data, "stickyBottom", false)
    stickyRight = get(entity.data, "stickyRight", false)
    stickyLeft = get(entity.data, "stickyLeft", false)
    if (stickyTop)
        Ahorn.drawImage(ctx, topTexture, -32 / 2, -32 / 2)
    end
    if (stickyBottom)
        Ahorn.Cairo.save(ctx)
        Ahorn.scale(ctx, 1, -1)
        Ahorn.drawImage(ctx, bottomTexture, -32 / 2, -32 / 2)
        Ahorn.Cairo.restore(ctx)
    end
    if (stickyRight)
        Ahorn.Cairo.save(ctx)
        Ahorn.rotate(ctx, pi/2)
        Ahorn.drawImage(ctx, rightTexture, -32 / 2, -32 / 2)
        Ahorn.Cairo.restore(ctx)
    end
    if (stickyLeft)
        Ahorn.Cairo.save(ctx)
        Ahorn.rotate(ctx, -pi/2)
        Ahorn.drawImage(ctx, leftTexture, -32 / 2, -32 / 2)
        Ahorn.Cairo.restore(ctx)
    end
end

function Ahorn.selection(entity::GrannyTemple)
    x, y = Ahorn.position(entity)

    return Ahorn.getSpriteRectangle("characters/oldlady/idle00.png", x, y, jx=0.5, jy=1.0)
end

Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::GrannyTemple) = Ahorn.drawSprite(ctx, "characters/oldlady/idle00.png", 0, 0, jx=0.5, jy=1)

function Ahorn.selection(entity::GrannyMonster)
    x, y = Ahorn.position(entity)

    return Ahorn.getSpriteRectangle("characters/oldlady/idle00.png", x, y, jx=0.5, jy=1.0)
end

Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::GrannyMonster) = Ahorn.drawSprite(ctx, "characters/oldlady/idle00.png", 0, 0, jx=0.5, jy=1)

function Ahorn.selection(entity::GrannyStart)
    x, y = Ahorn.position(entity)

    return Ahorn.getSpriteRectangle("characters/oldlady/idle00.png", x, y, jx=0.5, jy=1.0)
end

Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::GrannyStart) = Ahorn.drawSprite(ctx, "characters/oldlady/idle00.png", 0, 0, jx=0.5, jy=1)

function Ahorn.selection(entity::UnbreakableStone)
    x, y = Ahorn.position(entity)

    return Ahorn.getSpriteRectangle("objects/canyon/pushblock/intact.png", x, y, jx=0.5, jy=1.0)
end

Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::UnbreakableStone) = Ahorn.drawSprite(ctx, "objects/canyon/pushblock/intact.png", 0, 0, jx=0.5, jy=1)

end