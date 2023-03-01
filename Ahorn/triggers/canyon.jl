module CanyonTriggers

using ..Ahorn, Maple

const defaultTriggerWidth = 16
const defaultTriggerHeight = 16

@mapdef Trigger "canyon/monstertrigger" MonsterTrigger(x::Integer, y::Integer, width::Integer=defaultTriggerWidth, height::Integer=defaultTriggerHeight, animationName::String="idle", spawnPosX::Integer=0, spawnPosY::Integer=0, faceRight::Bool=false)
@mapdef Trigger "canyon/templefalltrigger" TempleFallTrigger(x::Integer, y::Integer, width::Integer=defaultTriggerWidth, height::Integer=defaultTriggerHeight)

const placements = Ahorn.PlacementDict(
    "Monster Trigger (Canyon)" => Ahorn.EntityPlacement(
        MonsterTrigger,
        "rectangle"
    ),
    "Temple Fall Trigger (Canyon)" => Ahorn.EntityPlacement(
        TempleFallTrigger,
        "rectangle"
    )
)

end