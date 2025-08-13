/*
  LinearStepperController library
  Author: T-Kuhn.
  Sapporo, November, 2024. Released into the public domain.
*/

#ifndef LinearStepperController_h
#define LinearStepperController_h

#include "Constants.h"
#include "Arduino.h"
#include "LinearStepper.h"
#include "MoveBatch.h"

class LinearStepperController
{
public:
  LinearStepperController(bool repeat);
  bool update();
  void attach(LinearStepper* sStepper);
  void resetMoveBatchExecution();
  MoveBatch moveBatches[MAX_NUM_OF_MOVEBATCHES];

private:
  void setFrequencyFrom(float moveDuration);
  bool _isExecutingBatch;
  bool _endlessRepeat;
  uint8_t _numOfAttachedSteppers;
  uint32_t _counter = 0;
  uint32_t _currentMoveBatchIndex = 0;
  LinearStepper* _linearSteppers[MAX_NUM_OF_STEPPERS];
  float _frequency;
};

#endif