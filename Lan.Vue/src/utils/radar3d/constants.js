export const DEFAULT_MAX_TRAIL = 2000
export const DEFAULT_SIM_SPEED = 1.0
export const DEFAULT_MAP_OPACITY = 0.90
export const UI_UPDATE_INTERVAL = 0.1

export const COLORS = {
  axes: { x: 0xff5149, y: 0x3fb950, z: 0x58a6ff },
  rings: 0x55aadd,
  sweep: 0x4488ff,
  stars: 0xc8d7f0,
}

export const DRONE_COLORS = [
  { hex: 0xff5149, name: '目标 α' },
  { hex: 0x58a6ff, name: '目标 β' },
  { hex: 0x3fb950, name: '目标 γ' },
  { hex: 0xffaa00, name: '目标 δ' },
  { hex: 0xcc44ff, name: '目标 ε' },
  { hex: 0x00ccaa, name: '目标 ζ' },
  { hex: 0xff6699, name: '目标 η' },
  { hex: 0x66ccff, name: '目标 θ' },
]

// Three.js坐标: X=东, Y=高, Z=北. 用户坐标: x=X, y=Z, z=Y
export const CAMERA_PRESETS = {
  default: { pos: [0, 18, -45], target: [0, 0, 0] },
  top:     { pos: [0, 50, 0],   target: [0, 0, 0] },
  front:   { pos: [0, 8, -40],  target: [0, 0, 0] },
}
