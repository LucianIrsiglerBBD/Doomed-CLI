# Swaps directional ASCII chars when mirroring a sprite: < > / \ ( )
swaps = str.maketrans('<>/\\()', '><\\/)(')

pairs = [
    (
        'Assets/Sprites/Horizontal/Right/base_enemy_horizontal_right.txt',
        'Assets/Sprites/Horizontal/Left/base_enemy_horizontal_left.txt',
    ),
    (
        'Assets/Sprites/Horizontal/Right/base_player_horizontal_right.txt',
        'Assets/Sprites/Horizontal/Left/base_player_horizontal_left.txt',
    ),
    (
        'Assets/Sprites/Horizontal/Right/base_pistol_horizontal_right.txt',
        'Assets/Sprites/Horizontal/Left/base_pistol_horizontal_left.txt',
    ),
    (
        'Assets/Sprites/Horizontal/Right/base_shotgun_horizontal_right.txt',
        'Assets/Sprites/Horizontal/Left/base_shotgun_horizontal_left.txt',
    ),
]

for src, dst in pairs:
    with open(src, encoding='utf-8') as f:
        lines = f.read().splitlines()

    width = max(len(l.rstrip()) for l in lines)
    normalized = [l.rstrip().ljust(width) for l in lines]

    with open(src, 'w', encoding='utf-8') as f:
        f.write('\n'.join(normalized))

    mirrored = '\n'.join(l[::-1].translate(swaps) for l in normalized)

    with open(dst, 'w', encoding='utf-8') as f:
        f.write(mirrored)

    print(f'done: {src.split("/")[-1]} -> {dst.split("/")[-1]}')
