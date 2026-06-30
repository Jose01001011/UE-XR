"""
npz_to_bvh.py  —  Batch converts ALL Kimodo/somaskel77 .npz files produced by
                   GEM-X into BVH animation files that can be imported into Blender.

SETUP:
  1. Install numpy and scipy into your Python environment:
       pip install numpy scipy
  2. Edit the two paths in the "Configure" block below to match your machine.
  3. Run:
       python npz_to_bvh.py

Then in Blender for each .bvh file:
  File -> Import -> Motion Capture (.bvh)
  Import settings: Scale = 0.01, Forward = -Z, Up = Y
"""

import numpy as np
from scipy.spatial.transform import Rotation
from collections import defaultdict
from pathlib import Path

# ── Configure — edit these two paths for your machine ─────────────────────────
# Folder where GEM-X wrote its outputs (contains subfolders, each with mocap_for_kimodo.npz)
GEM_X_OUTPUTS = r"C:\ai-tools\GEM-X\outputs\demo_soma"

# Folder where this script will write .bvh files (created automatically if missing)
OUTPUT_DIR    = r"C:\ai-tools\bvh_output"

FPS = 30.0
# ─────────────────────────────────────────────────────────────────────────────

# Joint names in npz order (index 0 = Hips, index 76 = RightToeEnd)
JOINT_NAMES = [
    "Hips",
    "Spine1", "Spine2", "Chest",
    "Neck1", "Neck2", "Head", "HeadEnd", "Jaw", "LeftEye", "RightEye",
    "LeftShoulder", "LeftArm", "LeftForeArm", "LeftHand",
    "LeftHandThumb1", "LeftHandThumb2", "LeftHandThumb3", "LeftHandThumbEnd",
    "LeftHandIndex1", "LeftHandIndex2", "LeftHandIndex3", "LeftHandIndex4", "LeftHandIndexEnd",
    "LeftHandMiddle1", "LeftHandMiddle2", "LeftHandMiddle3", "LeftHandMiddle4", "LeftHandMiddleEnd",
    "LeftHandRing1", "LeftHandRing2", "LeftHandRing3", "LeftHandRing4", "LeftHandRingEnd",
    "LeftHandPinky1", "LeftHandPinky2", "LeftHandPinky3", "LeftHandPinky4", "LeftHandPinkyEnd",
    "RightShoulder", "RightArm", "RightForeArm", "RightHand",
    "RightHandThumb1", "RightHandThumb2", "RightHandThumb3", "RightHandThumbEnd",
    "RightHandIndex1", "RightHandIndex2", "RightHandIndex3", "RightHandIndex4", "RightHandIndexEnd",
    "RightHandMiddle1", "RightHandMiddle2", "RightHandMiddle3", "RightHandMiddle4", "RightHandMiddleEnd",
    "RightHandRing1", "RightHandRing2", "RightHandRing3", "RightHandRing4", "RightHandRingEnd",
    "RightHandPinky1", "RightHandPinky2", "RightHandPinky3", "RightHandPinky4", "RightHandPinkyEnd",
    "LeftLeg", "LeftShin", "LeftFoot", "LeftToeBase", "LeftToeEnd",
    "RightLeg", "RightShin", "RightFoot", "RightToeBase", "RightToeEnd",
]

# Parent relationships (None = BVH root)
PARENT = {
    "Hips": None,
    "Spine1": "Hips",  "Spine2": "Spine1", "Chest": "Spine2",
    "Neck1": "Chest",  "Neck2": "Neck1",   "Head": "Neck2",
    "HeadEnd": "Head", "Jaw": "Head", "LeftEye": "Head", "RightEye": "Head",
    "LeftShoulder": "Chest",
    "LeftArm": "LeftShoulder",   "LeftForeArm": "LeftArm",   "LeftHand": "LeftForeArm",
    "LeftHandThumb1": "LeftHand",   "LeftHandThumb2": "LeftHandThumb1",
    "LeftHandThumb3": "LeftHandThumb2", "LeftHandThumbEnd": "LeftHandThumb3",
    "LeftHandIndex1": "LeftHand",   "LeftHandIndex2": "LeftHandIndex1",
    "LeftHandIndex3": "LeftHandIndex2", "LeftHandIndex4": "LeftHandIndex3",
    "LeftHandIndexEnd": "LeftHandIndex4",
    "LeftHandMiddle1": "LeftHand",  "LeftHandMiddle2": "LeftHandMiddle1",
    "LeftHandMiddle3": "LeftHandMiddle2", "LeftHandMiddle4": "LeftHandMiddle3",
    "LeftHandMiddleEnd": "LeftHandMiddle4",
    "LeftHandRing1": "LeftHand",    "LeftHandRing2": "LeftHandRing1",
    "LeftHandRing3": "LeftHandRing2", "LeftHandRing4": "LeftHandRing3",
    "LeftHandRingEnd": "LeftHandRing4",
    "LeftHandPinky1": "LeftHand",   "LeftHandPinky2": "LeftHandPinky1",
    "LeftHandPinky3": "LeftHandPinky2", "LeftHandPinky4": "LeftHandPinky3",
    "LeftHandPinkyEnd": "LeftHandPinky4",
    "RightShoulder": "Chest",
    "RightArm": "RightShoulder", "RightForeArm": "RightArm", "RightHand": "RightForeArm",
    "RightHandThumb1": "RightHand",  "RightHandThumb2": "RightHandThumb1",
    "RightHandThumb3": "RightHandThumb2", "RightHandThumbEnd": "RightHandThumb3",
    "RightHandIndex1": "RightHand",  "RightHandIndex2": "RightHandIndex1",
    "RightHandIndex3": "RightHandIndex2", "RightHandIndex4": "RightHandIndex3",
    "RightHandIndexEnd": "RightHandIndex4",
    "RightHandMiddle1": "RightHand", "RightHandMiddle2": "RightHandMiddle1",
    "RightHandMiddle3": "RightHandMiddle2", "RightHandMiddle4": "RightHandMiddle3",
    "RightHandMiddleEnd": "RightHandMiddle4",
    "RightHandRing1": "RightHand",   "RightHandRing2": "RightHandRing1",
    "RightHandRing3": "RightHandRing2", "RightHandRing4": "RightHandRing3",
    "RightHandRingEnd": "RightHandRing4",
    "RightHandPinky1": "RightHand",  "RightHandPinky2": "RightHandPinky1",
    "RightHandPinky3": "RightHandPinky2", "RightHandPinky4": "RightHandPinky3",
    "RightHandPinkyEnd": "RightHandPinky4",
    "LeftLeg": "Hips",   "LeftShin": "LeftLeg",   "LeftFoot": "LeftShin",
    "LeftToeBase": "LeftFoot", "LeftToeEnd": "LeftToeBase",
    "RightLeg": "Hips",  "RightShin": "RightLeg", "RightFoot": "RightShin",
    "RightToeBase": "RightFoot", "RightToeEnd": "RightToeBase",
}

# T-pose offsets from somaskel77_standard_tpose.bvh (in centimetres).
# Hips is the BVH ROOT — its offset is (0,0,0); position comes from motion data.
OFFSET_CM = {
    "Hips":               (0.0, 0.0, 0.0),
    "Spine1":             (-0.013727, 5.003763, -0.053727),
    "Spine2":             (-0.0, 7.125301, -0.029825),
    "Chest":              (-1e-06, 7.550063, -0.815971),
    "Neck1":              (-0.181677, 26.311295, -0.553348),
    "Neck2":              (-3e-06, 7.709397, 2.302585),
    "Head":               (-5e-06, 6.128916, 1.953709),
    "HeadEnd":            (0.003598, 16.065403, -1.835379),
    "Jaw":                (0.002637, 0.475592, 3.094941),
    "LeftEye":            (3.206381, 5.380205, 7.586883),
    "RightEye":           (-3.22244, 5.361869, 7.558234),
    "LeftShoulder":       (1.621652, 23.237164, 5.113413),
    "LeftArm":            (14.919846, 2e-06, -5.502326),
    "LeftForeArm":        (28.739307, 0.0, -0.002588),
    "LeftHand":           (27.093981, -1e-06, 0.002609),
    "LeftHandThumb1":     (2.276482, -1.392045, 3.191413),
    "LeftHandThumb2":     (4.012836, -1.828127, 1.641654),
    "LeftHandThumb3":     (2.798515, 0.0, -3e-06),
    "LeftHandThumbEnd":   (3.180793, -4e-06, 4e-06),
    "LeftHandIndex1":     (3.247555, -0.531998, 2.296169),
    "LeftHandIndex2":     (6.364578, 0.01206, 0.1786),
    "LeftHandIndex3":     (3.662364, 0.0, 0.0),
    "LeftHandIndex4":     (2.329242, 4e-06, 4e-06),
    "LeftHandIndexEnd":   (2.759615, -0.180537, -0.113024),
    "LeftHandMiddle1":    (3.163495, 0.240981, 1.000332),
    "LeftHandMiddle2":    (6.19078, -0.259278, -1.002548),
    "LeftHandMiddle3":    (4.35652, -4e-06, -1e-06),
    "LeftHandMiddle4":    (2.996877, -8e-06, 0.0),
    "LeftHandMiddleEnd":  (2.304287, -0.294569, -0.031741),
    "LeftHandRing1":      (2.882643, -0.053652, -0.322543),
    "LeftHandRing2":      (5.854541, -0.486202, -1.373841),
    "LeftHandRing3":      (4.350578, 0.0, 3e-06),
    "LeftHandRing4":      (2.651321, 7e-06, 2e-06),
    "LeftHandRingEnd":    (1.936105, 0.077687, -7.1e-05),
    "LeftHandPinky1":     (2.8655, -0.310005, -1.600378),
    "LeftHandPinky2":     (5.087849, -1.331141, -1.77123),
    "LeftHandPinky3":     (3.070974, 4e-06, 0.0),
    "LeftHandPinky4":     (1.549672, 0.0, 1e-06),
    "LeftHandPinkyEnd":   (1.944893, -0.157802, 0.057219),
    "RightShoulder":      (-1.380118, 23.180309, 5.214158),
    "RightArm":           (-15.037196, 1.2e-05, -5.545604),
    "RightForeArm":       (-28.736639, 2e-06, -0.002597),
    "RightHand":          (-27.133619, -0.0, 0.002613),
    "RightHandThumb1":    (-2.274032, -1.383988, 3.163127),
    "RightHandThumb2":    (-4.011429, -1.827466, 1.640914),
    "RightHandThumb3":    (-2.794935, -4e-06, -3e-06),
    "RightHandThumbEnd":  (-3.183852, 4e-06, 1e-06),
    "RightHandIndex1":    (-3.253266, -0.520057, 2.282866),
    "RightHandIndex2":    (-6.341917, 0.012471, 0.178266),
    "RightHandIndex3":    (-3.654871, -8e-06, -0.0),
    "RightHandIndex4":    (-2.327586, 0.0, 1e-06),
    "RightHandIndexEnd":  (-2.76179, -0.180656, -0.113078),
    "RightHandMiddle1":   (-3.168106, 0.246593, 1.00103),
    "RightHandMiddle2":   (-6.180828, -0.258836, -1.000895),
    "RightHandMiddle3":   (-4.348901, 0.0, -0.0),
    "RightHandMiddle4":   (-3.00024, -4e-06, -2e-06),
    "RightHandMiddleEnd": (-2.30252, -0.29437, -0.031706),
    "RightHandRing1":     (-2.88569, -0.067952, -0.308858),
    "RightHandRing2":     (-5.854198, -0.48613, -1.373731),
    "RightHandRing3":     (-4.33881, -4e-06, -0.0),
    "RightHandRing4":     (-2.654903, -4e-06, 4e-06),
    "RightHandRingEnd":   (-1.933568, 0.077527, -5.2e-05),
    "RightHandPinky1":    (-2.866425, -0.342796, -1.584145),
    "RightHandPinky2":    (-5.091371, -1.332055, -1.772385),
    "RightHandPinky3":    (-3.062664, -4e-06, 1e-06),
    "RightHandPinky4":    (-1.546529, 4e-06, -2e-06),
    "RightHandPinkyEnd":  (-1.945119, -0.157718, 0.057211),
    "LeftLeg":            (10.043214, -8.434526, 2.595655),
    "LeftShin":           (-1e-06, -43.221752, -0.802913),
    "LeftFoot":           (1e-06, -42.155094, -3.481523),
    "LeftToeBase":        (0.0, -5.059472, 13.231529),
    "LeftToeEnd":         (-0.009607, -1.647619, 6.513017),
    "RightLeg":           (-10.047278, -8.29526, 2.620317),
    "RightShin":          (1e-06, -43.362206, -0.805556),
    "RightFoot":          (2e-06, -42.117393, -3.478398),
    "RightToeBase":       (-0.0, -5.079609, 13.284196),
    "RightToeEnd":        (0.009532, -1.634378, 6.460591),
}

# Build children map in JOINT_NAMES insertion order (preserves DFS order)
CHILDREN = defaultdict(list)
for jname in JOINT_NAMES:
    p = PARENT[jname]
    if p is not None:
        CHILDREN[p].append(jname)

NAME_TO_IDX = {name: i for i, name in enumerate(JOINT_NAMES)}


def write_hierarchy(f, joint, indent=0):
    pad = "  " * indent
    ox, oy, oz = OFFSET_CM[joint]
    children = CHILDREN[joint]

    keyword = "ROOT" if indent == 0 else "JOINT"
    f.write(f"{pad}{keyword} {joint}\n")
    f.write(f"{pad}{{\n")
    f.write(f"{pad}  OFFSET {ox:.6f} {oy:.6f} {oz:.6f}\n")

    if indent == 0:
        f.write(f"{pad}  CHANNELS 6 Xposition Yposition Zposition Zrotation Yrotation Xrotation\n")
    else:
        f.write(f"{pad}  CHANNELS 3 Zrotation Yrotation Xrotation\n")

    if children:
        for child in children:
            write_hierarchy(f, child, indent + 1)
    else:
        f.write(f"{pad}  End Site\n")
        f.write(f"{pad}  {{\n")
        f.write(f"{pad}    OFFSET 0.0 1.0 0.0\n")
        f.write(f"{pad}  }}\n")

    f.write(f"{pad}}}\n")


def rot_to_zyx_deg(mat3x3):
    """3x3 rotation matrix -> (Zrot, Yrot, Xrot) in degrees (BVH ZYX Euler order)."""
    zyx = Rotation.from_matrix(mat3x3).as_euler('ZYX', degrees=True)
    return float(zyx[0]), float(zyx[1]), float(zyx[2])


def build_dfs_order():
    """Return non-root joints in DFS order matching the hierarchy block."""
    order = []
    def dfs(j):
        order.append(j)
        for c in CHILDREN[j]:
            dfs(c)
    for c in CHILDREN["Hips"]:
        dfs(c)
    return order


def convert_npz_to_bvh(npz_path, out_path, dfs_joints):
    data = np.load(npz_path)
    local_rot_mats = data['local_rot_mats']   # (T, 77, 3, 3)
    root_positions = data['smooth_root_pos']   # (T, 3) in metres
    T = local_rot_mats.shape[0]

    with open(out_path, 'w') as f:
        f.write("HIERARCHY\n")
        write_hierarchy(f, "Hips", indent=0)
        f.write("MOTION\n")
        f.write(f"Frames: {T}\n")
        f.write(f"Frame Time: {1.0/FPS:.6f}\n")

        for frame in range(T):
            row = []
            px, py, pz = root_positions[frame] * 100.0   # metres -> cm
            rz, ry, rx = rot_to_zyx_deg(local_rot_mats[frame, 0])
            row += [px, py, pz, rz, ry, rx]
            for jname in dfs_joints:
                idx = NAME_TO_IDX[jname]
                rz, ry, rx = rot_to_zyx_deg(local_rot_mats[frame, idx])
                row += [rz, ry, rx]
            f.write(" ".join(f"{v:.6f}" for v in row) + "\n")

            if (frame + 1) % 100 == 0:
                print(f"    frame {frame+1}/{T}")

    return T


def main():
    import glob

    out_dir = Path(OUTPUT_DIR)
    out_dir.mkdir(parents=True, exist_ok=True)

    # Find all mocap_for_kimodo.npz files under GEM_X_OUTPUTS
    pattern = str(Path(GEM_X_OUTPUTS) / "**" / "mocap_for_kimodo.npz")
    npz_files = sorted(glob.glob(pattern, recursive=True))

    if not npz_files:
        print(f"No mocap_for_kimodo.npz files found under:\n  {GEM_X_OUTPUTS}")
        print("\nMake sure GEM-X has finished running and GEM_X_OUTPUTS points to the right folder.")
        return

    print(f"Found {len(npz_files)} animation(s):\n")
    dfs_joints = build_dfs_order()

    for npz_path in npz_files:
        anim_name = Path(npz_path).parent.name
        out_path  = out_dir / f"{anim_name}.bvh"

        print(f"  [{anim_name}]")
        print(f"    NPZ  -> {npz_path}")
        print(f"    BVH  -> {out_path}")

        T = convert_npz_to_bvh(npz_path, str(out_path), dfs_joints)
        print(f"    Done -- {T} frames\n")

    print("=" * 60)
    print(f"All BVH files saved to:\n  {OUTPUT_DIR}")
    print()
    print("Import each into Blender:")
    print("  File -> Import -> Motion Capture (.bvh)")
    print(f"  Navigate to: {OUTPUT_DIR}")
    print("  Import settings: Scale = 0.01, Forward = -Z, Up = Y")


if __name__ == "__main__":
    main()
