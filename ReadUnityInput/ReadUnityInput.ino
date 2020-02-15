#include <Servo.h>

Servo servoPhi;
Servo servoTheta;

const byte numChars = 32;
char receivedChars[numChars];
char tempChars[numChars];        // temporary array for use when parsing
boolean newData = false;

int phiRotation = 90;
int thetaRotation = 90;

void setup() {
  Serial.begin (9600);
  servoPhi.attach(2);
  servoTheta.attach(5);
  
}


void loop()
{

  recvWithStartEndMarkers();
  if (newData == true) {
      strcpy(tempChars, receivedChars);
          // this temporary copy is necessary to protect the original data
          //   because strtok() used in parseData() replaces the commas with \0
      parseData();
      moveServos(phiRotation, thetaRotation);
      newData = false;
  }
  /*
  //Must delay 1 milliseconds to make Serial.available() work properly
  delay(1);

  //Serial.setTimeout(0);

  if ( Serial.available() >= 2) {

    servoInputAngles= Serial.readString();
    Serial.println(servoInputAngles);
    //moveServos(servoInputAngles[0],servoInputAngles[1]);
  }*/


}

void parseData() {      // split the data into its parts

    char * strtokIndx; // this is used by strtok() as an index

    strtokIndx = strtok(tempChars,",");      // get the first part - the phi rotation
    phiRotation = atoi(strtokIndx);     // convert this part to an integer

    strtokIndx = strtok(NULL, ","); // this continues where the previous call left off
    thetaRotation = atoi(strtokIndx);     // convert this part to an integer

}

void recvWithStartEndMarkers() {
    static boolean recvInProgress = false;
    static byte ndx = 0;
    char startMarker = '<';
    char endMarker = '>';
    char rc;

    while (Serial.available() > 0 && newData == false) {
        rc = Serial.read();

        if (recvInProgress == true) {
            if (rc != endMarker) {
                receivedChars[ndx] = rc;
                ndx++;
                if (ndx >= numChars) {
                    ndx = numChars - 1;
                }
            }
            else {
                receivedChars[ndx] = '\0'; // terminate the string
                recvInProgress = false;
                ndx = 0;
                newData = true;
            }
        }

        else if (rc == startMarker) {
            recvInProgress = true;
        }
    }
    //Serial.println(phiRotation);
}


void moveServos(int phiAngle, int thetaAngle){
  servoPhi.write(phiAngle);
  servoTheta.write(thetaAngle);
  
}
