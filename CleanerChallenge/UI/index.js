
const dust = document.getElementById('dust').getContext("2d", { willReadFrequently: true })
const overlay = document.getElementById('overlay')

function mulberry32(a) {
    return function () {
        var t = a += 0x6D2B79F5;
        t = Math.imul(t ^ t >>> 15, t | 1);
        t ^= t + Math.imul(t ^ t >>> 7, t | 61);
        return ((t ^ t >>> 14) >>> 0) / 4294967296;
    }
}

class Robot {
    constructor(x, y) {
        this.x = x
        this.y = y
        this.a = 0
        this.dom = document.getElementById('robot')
    }

    draw() {
        dust.beginPath()
        dust.arc(this.x, this.y, 32, 0, 2 * Math.PI)
        dust.fillStyle = '#fff';
        dust.fill();

        dust.globalCompositeOperation = 'destination-out'
        dust.drawImage(overlay, this.x - 33, this.y - 33, 66, 66, this.x - 33, this.y - 33, 66, 66)
        dust.globalCompositeOperation = 'source-over'

        this.dom.style.transform = `translate(${this.x}px, ${this.y}px) rotate(${this.a}rad)`
    }

    collect(socks) {
        const R2 = 24 * 24
        let count = 0

        for (let i = socks.length; i-- > 0;) {
            const dx = this.x - socks[i].x
            const dy = this.y - socks[i].y
            if (dx * dx + dy * dy <= R2) {
                socks[i].dom.remove()
                socks.splice(i, 1)
                count++
            }
        }
        return count
    }

    collides(x, y) {
        const data = dust.getImageData(x - 32, y - 32, 64, 64).data
        const R2 = 32 * 32
        for (let i = -32, idx = 0; i < 32; i++) {
            for (let j = -32; j < 32; j++, idx += 4) {
                if (data[idx] == 0 && i * i + j * j < R2) {
                    return true
                }
            }
        }
        return false
    }

    moveTo = function* (x, y) {
        x = Math.floor(x)
        y = Math.floor(y)

        const ta = Math.atan2(y - this.y, x - this.x)
        const dx = Math.abs(x - this.x)
        const sx = this.x < x ? 1 : -1
        const dy = -Math.abs(y - this.y)
        const sy = this.y < y ? 1 : -1
        let error = dx + dy

        while (true) {
            this.draw()
            this.a += (ta - this.a) * 0.1

            if (this.x == x && this.y == y) {
                break
            }
            const e2 = error * 2
            if (e2 >= dy) {
                if (this.x == x) {
                    break
                }
                if (this.collides(this.x + sx, this.y)) {
                    break;
                }
                error += dy
                this.x += sx
            }
            if (e2 <= dx) {
                if (this.y == y) {
                    break
                }
                if (this.collides(this.x, this.y + sy)) {
                    break;
                }
                error += dx
                this.y += sy
            }
            yield
        }
    }
}

class Room {
    constructor(seed, count) {
        const socks = document.getElementById('socks')
        const random = mulberry32(seed)

        dust.fillStyle = '#010000'
        dust.fillRect(0, 0, 1280, 800)

        dust.globalCompositeOperation = 'destination-out'
        dust.drawImage(overlay, 0, 0, 1280, 800)
        dust.globalCompositeOperation = 'source-over'

        const data = dust.getImageData(0, 0, 1280, 800).data

        this.socks = []
        while (this.socks.length < count) {
            const x = Math.floor(random() * 1200 + 40)
            const y = Math.floor(random() * 700 + 50)
            const idx = (y * 1280 + x) * 4

            if (data[idx] > 0) {
                const dom = document.createElement('img')
                dom.classList.add('sock')
                dom.width = dom.height = 32
                dom.src = 'socks.png'
                dom.style.transform = `translate(${x}px, ${y}px) rotate(${Math.random() * 360}deg)`
                socks.appendChild(dom)
                this.socks.push({ x, y, dom })
            }
        }
    }
}

class Timer {
    constructor(seconds) {
        this.timeout = seconds * 60
        this.dt = 1
    }

    toString() {
        const min = Math.floor(this.timeout / 60 / 60)
        const sec = Math.floor(this.timeout / 60 % 60)
        return sec < 10 ? `${min}:0${sec}` : `${min}:${sec}`
    }

    tick(count = 1) {
        return (this.timeout -= this.dt * count) > 0
    }

    frame() {
        return new Promise(resolve => requestAnimationFrame(resolve))
    }
}

async function RunGame() {
    const seed = await fetch('/api/reset')
    const score = document.getElementById('score')
    const time = document.getElementById('time')
    const robot = new Robot(100, 200)
    const room = new Room(await seed.json(), 21)
    const timer = new Timer(5 * 60)

    function calcScore() {
        const data = dust.getImageData(0, 0, 1280, 800).data
        let score = 0
        for (let i = 0; i < data.length; i += 4) {
            score += data[i] / 255
        }
        return Math.ceil(score)
    }

    function createState() {
        const data = dust.getImageData(0, 0, 1280, 800).data
        const d = new Array(data.length / 4)
        for (let i = 0; i < d.length; i++) {
            d[i] = String.fromCharCode(data[i * 4])
        }
        return {
            timer: timer,
            score: calcScore(),
            dust: btoa(d.join('')),
            robot: { x: robot.x, y: robot.y },
            socks: room.socks.map(sock => ({ x: sock.x, y: sock.y }))
        }
    }

    robot.draw()

    while (true) {
        try {
            const state = createState()
            const response = await fetch('/api/next', {
                headers: {
                    'Accept': 'application/json',
                    'Content-Type': 'application/json'
                },
                method: 'POST',
                body: JSON.stringify(state)
            })
            const command = await response.json()

            score.innerText = state.score
            time.innerText = timer.toString()

            if (!timer.tick(3)) {
                return GameOver(score.innerText)
            }

            const it = robot.moveTo(command.x, command.y)
            for (let step = it.next(); !step.done; step = it.next()) {
                const collected = robot.collect(room.socks)
                if (collected > 0) {
                    timer.dt += collected * 0.5
                }
                if (!timer.tick()) {
                    score.innerText = calcScore()
                    time.innerText = '0:00'
                    return GameOver(score.innerText)
                }
                if (!command.fast) {
                    score.innerText = calcScore()
                    time.innerText = timer.toString()
                    await timer.frame()
                }
            }
        }
        catch (e) {
            console.error(e)
        }
    }
}

async function GameOver(score) {
    try {
        const response = await fetch('/api/restart', {
            headers: {
                'Accept': 'application/json',
                'Content-Type': 'application/json'
            },
            method: 'POST',
            body: JSON.stringify({ score })
        })
        const restart = await response.json()
        if (restart) {
            location.reload()
        }
    }
    catch (e) {
        console.error(e)
    }
}

overlay.complete ? RunGame() : (overlay.onload = RunGame)
