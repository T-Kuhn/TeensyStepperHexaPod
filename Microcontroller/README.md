# Teensy Stepper Experiments
Running some tests with a Teensy 4.0 and a Nema23 stepper motor with a CL57T driver. Merely some personal notes.

## Settings

Experiment | Microsteps | Amps | Driver PID Params | Encoder ON/OFF |
-----------|-----------|----------|----------|----------|
| A | 40'000    | 2A | 0/25/25 | ON |
| B | 40'000    | 2A | 16/50/15 | ON |
| C | 51'200    | 4A | 0/25/25 | ON |
| D | 51'200    | 4A | 16/100/5 | ON |
| E | 51'200    | 2A | 0/25/25 | ON |
| F | 25'600    | 2A | 0/25/25 | ON |
| G | 12'800    | 2A | 0/25/25 | ON |
| H | 12'800    | 4A | 0/25/25 | ON |

## Observations

- R1: No big/easily detectable difference between A and B
- R2: Comparing C and D: Audible "tock" sound with D on motion start (if constant load was applied). C seemed more smooth.
- R3: Going 6'366 pulses in 30ms seems to be limit for pulse generation.
- R4: Going 6'366 pulses in 40ms still works.
- R5: G: Super fast. But Power source turns off (too many amps drawn?)
- R6: Same with H (poser source turns off). Seems like we have reached some limit with the power source.

## Results

- Power source turns off if we try to drive the motor with high acceleration and speed (see R5 and R6)
- Our micro controller can't handle (can't keep up with the pulse generation) if we try to move 6'366 Steps in 30ms)
  - 6'366 pulses in 30ms turns out to be 212'000 pulses per second (we need pulses generated at 212Khz. It's actually a bit more, since our speed curve isn't linear, but starts and stops smoothly...)
  - Seeing that it stopped working at 212kHz makes perfect sense, since we expect the max pulse/sec rate to be higher (since we start and stop in a sinoidal manner) and we consider that our pulse generation timer runs at 2μs and thus our max pulses per second is 250kHz.