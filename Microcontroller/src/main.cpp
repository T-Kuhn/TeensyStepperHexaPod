/*
 * Blink
 * Turns on an LED on for one second,
 * then off for one second, repeatedly.
 */

#include <Arduino.h>
#include "Constants.h"
#include "SineStepper.h"
#include "SineStepperController.h"
#include "MoveBatch.h"

enum Mode
{
    idle,
    doingControlledMovements,
    error
};

Mode currentMode = idle;

SineStepper sineStepper1(STEPPER1_STEP_PIN, STEPPER1_DIR_PIN, /*id:*/ 0);
SineStepper sineStepper2(STEPPER2_STEP_PIN, STEPPER2_DIR_PIN, /*id:*/ 1);
SineStepperController sineStepperController(/*endlessRepeat:*/ false);
IntervalTimer myTimer;

void onTimer()
{
    digitalWrite(EXECUTING_ISR_CODE, HIGH);

    switch (currentMode)
    {
    case idle:
        break;
    case doingControlledMovements:
        sineStepperController.update();
        digitalWrite(EXECUTING_ISR_CODE, LOW);
        break;
    default:
        break;
    }
}

void setup()
{
    pinMode(EXECUTING_ISR_CODE, OUTPUT);

    myTimer.begin(onTimer, TIMER_US);

    sineStepperController.attach(&sineStepper1);
    sineStepperController.attach(&sineStepper2);
}

void loop()
{
    delay(4200);

    currentMode = idle;
    delay(1000);

    MoveBatch* mb = &sineStepperController.moveBatches[0];
    mb->addMove(/*id:*/ 0, /*pos:*/ (int32_t)(PULSES_PER_REV * 25.0 / (M_PI * 2)));
    mb->addMove(/*id:*/ 1, /*pos:*/ (int32_t)(-PULSES_PER_REV * 25.0 / (M_PI * 2)));
    mb->moveDuration = 1;

    MoveBatch* mb2 = &sineStepperController.moveBatches[1];
    mb2->addMove(/*id:*/ 0, /*pos:*/ (int32_t)(PULSES_PER_REV * 1 / (M_PI * 2)));
    mb2->addMove(/*id:*/ 1, /*pos:*/ (int32_t)(-PULSES_PER_REV * 1 / (M_PI * 2)));
    mb2->moveDuration = 1;

    sineStepperController.resetMoveBatchExecution();
    currentMode = doingControlledMovements;
}