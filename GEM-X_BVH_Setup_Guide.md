# GEM-X to BVH Setup Guide

Convert a regular video of a person moving into a BVH animation file you can import into Blender.

**What this pipeline does:**
Video (.mp4) → **GEM-X** (NVIDIA AI) → `.npz` mocap data → **npz_to_bvh.py** → `.bvh` → **Blender**

---

## Prerequisites

| Requirement | Notes |
|---|---|
| Windows 10/11 (64-bit) | Linux also works — adjust paths accordingly |
| NVIDIA GPU | Recommended: RTX 3060+ with 8 GB VRAM. CPU-only is possible but very slow |
| NVIDIA drivers | Check with `nvidia-smi` in a terminal. Must support CUDA 12.6+ |
| Python 3.12 | Download from python.org. Tick "Add Python to PATH" during install |
| Git | Download from git-scm.com |
| Git LFS | Required for GEM-X model assets — see Step 1 |
| uv | Fast Python package manager (replaces pip for GEM-X install) |
| Blender 3.6 or 4.x | For importing the final BVH |

> **Disk space:** GEM-X downloads about 6.7 GB of model files on first run. Make sure your drive has at least 15 GB free.

---

## Step 1 — Install Git LFS and uv

Open a terminal (PowerShell or Command Prompt) and run:

```bash
# Git LFS (needed to pull large model files from GitHub)
git lfs install

# uv (fast Python package manager used by GEM-X)
pip install uv
```

---

## Step 2 — Clone GEM-X

Pick a folder on a drive with enough space (e.g. `C:\ai-tools\GEM-X`):

```bash
git clone --recursive https://github.com/NVlabs/GEM-X.git C:\ai-tools\GEM-X
cd C:\ai-tools\GEM-X
```

> If you cloned without `--recursive`, run this inside the folder:
> ```bash
> git submodule update --init --recursive
> ```

---

## Step 3 — Create a Python virtual environment

Inside the `GEM-X` folder:

```bash
uv venv .venv --python 3.12
.venv\Scripts\activate
```

Your prompt should now show `(.venv)` at the start.

---

## Step 4 — Install PyTorch with CUDA

First check your CUDA version:

```bash
nvidia-smi
```

Look for "CUDA Version: XX.X" in the top-right of the output. Then install the matching PyTorch:

```bash
# CUDA 12.6 (most common for recent drivers)
uv pip install torch torchvision --index-url https://download.pytorch.org/whl/cu126

# CUDA 12.4
uv pip install torch torchvision --index-url https://download.pytorch.org/whl/cu124

# CUDA 13.0 (bleeding edge)
uv pip install torch torchvision --index-url https://download.pytorch.org/whl/cu130
```

---

## Step 5 — Install GEM-X and its dependencies

Still inside `C:\ai-tools\GEM-X` with `.venv` active:

```bash
# Install the SOMA body model (required — pulls LFS files)
uv pip install -e third_party/soma
cd third_party/soma
git lfs pull
cd ..\..

# Install GEM and everything else
bash scripts/install_env.sh
```

> **Windows note:** If `bash` is not found, use Git Bash (right-click the folder in Explorer > "Git Bash Here") or WSL.

Also install numpy and scipy for the BVH conversion script:

```bash
uv pip install numpy scipy
```

---

## Step 6 — Download the pretrained model (first run only)

GEM-X downloads the model automatically on first run (~6.7 GB to your Hugging Face cache). To keep it off your C: drive, set this environment variable before running:

**PowerShell:**
```powershell
$env:HF_HOME = "D:\huggingface_cache"
$env:TORCH_HOME = "D:\torch_cache"
```

**Command Prompt:**
```cmd
set HF_HOME=D:\huggingface_cache
set TORCH_HOME=D:\torch_cache
```

Or you can manually pre-download it:
```bash
huggingface-cli download nvidia/GEM-X gem_soma.ckpt --local-dir inputs/pretrained
```

---

## Step 7 — Run GEM-X on your video

Put your video somewhere accessible (e.g. `C:\ai-tools\GEM-X\inputs\my_video.mp4`), then:

```bash
cd C:\ai-tools\GEM-X
.venv\Scripts\activate
python scripts/demo/demo_soma.py --video "inputs/my_video.mp4"
```

> **First run:** This downloads ~6.7 GB of models. Leave it running — it can take 10–20 minutes.
> **Subsequent runs:** Much faster, usually a few minutes per video.

Output will be written to:
```
C:\ai-tools\GEM-X\outputs\demo_soma\<video_name>\mocap_for_kimodo.npz
```

---

## Step 8 — Configure the conversion script

Open `npz_to_bvh.py` (in the same folder as this guide) in any text editor and change the two paths at the top to match your machine:

```python
# Folder where GEM-X wrote its outputs
GEM_X_OUTPUTS = r"C:\ai-tools\GEM-X\outputs\demo_soma"

# Folder where .bvh files will be saved (created automatically)
OUTPUT_DIR    = r"C:\ai-tools\bvh_output"
```

---

## Step 9 — Convert NPZ to BVH

With `.venv` still active (or using any Python that has `numpy` and `scipy`):

```bash
python C:\ai-tools\npz_to_bvh.py
```

The script will find every `mocap_for_kimodo.npz` under your `GEM_X_OUTPUTS` folder and convert each one to a `.bvh` file named after its parent folder. Progress is printed per 100 frames.

---

## Step 10 — Import into Blender

1. Open Blender
2. **File > Import > Motion Capture (.bvh)**
3. Navigate to your `OUTPUT_DIR` and select the `.bvh` file
4. In the import options panel (bottom-left of the file browser), set:
   - **Scale:** `0.01`
   - **Forward:** `-Z`
   - **Up:** `Y`
5. Click **Import BVH**

The skeleton will appear in the scene with the full animation. You can then retarget it to any character rig using Blender's pose library or tools like Auto-Rig Pro.

---

## Troubleshooting

| Problem | Fix |
|---|---|
| `git lfs` files are just text pointer files | Run `cd third_party/soma && git lfs pull` |
| CUDA version mismatch error | Re-check `nvidia-smi` output and reinstall PyTorch for the correct version |
| `ModuleNotFoundError: gem` | Make sure `.venv` is activated and you ran `scripts/install_env.sh` |
| `No mocap_for_kimodo.npz files found` | Check that GEM-X finished without errors and that `GEM_X_OUTPUTS` in the script is correct |
| Animation looks squished/giant in Blender | Make sure Scale is `0.01` — not 1.0 — in the BVH import settings |
| No `.bvh` import option in Blender | Blender built-in BVH importer — go to Edit > Preferences > Add-ons and enable "Import-Export: BVH format" |
| OpenGL/EGL errors on Linux | Set `PYOPENGL_PLATFORM=egl` and `EGL_PLATFORM=surfaceless` before running |

---

## Quick Reference

```
# Activate venv (do this every time you open a new terminal)
cd C:\ai-tools\GEM-X
.venv\Scripts\activate

# Run GEM-X on a video
python scripts/demo/demo_soma.py --video "inputs/your_video.mp4"

# Convert output to BVH
python C:\ai-tools\npz_to_bvh.py

# Output BVH location
C:\ai-tools\bvh_output\<video_name>.bvh
```
