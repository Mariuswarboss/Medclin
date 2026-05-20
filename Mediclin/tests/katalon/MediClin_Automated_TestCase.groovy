import static com.kms.katalon.core.testobject.ObjectRepository.findWindowsObject

import com.kms.katalon.core.model.FailureHandling as FailureHandling
import com.kms.katalon.core.util.KeywordUtil as KeywordUtil
import com.kms.katalon.core.windows.keyword.WindowsBuiltinKeywords as Windows

/*
 * MediClin - script automat Katalon pentru testare Windows/WPF.
 *
 * Important:
 * 1. Inregistreaza mai intai pasii cu Native Windows Recorder.
 * 2. Salveaza elementele in Object Repository.
 * 3. Inlocuieste caile Login/Input_Email, Login/Input_Parola etc. cu numele reale generate de Katalon.
 */

String appPath = 'D:\\medclin\\Mediclin\\Mediclin.UI\\bin\\Debug\\net8.0-windows\\Mediclin.UI.exe'

String pacientEmail = '<email_pacient>'
String pacientParola = '<parola_pacient>'

try {
    KeywordUtil.logInfo('Pornire aplicatie MediClin')
    Windows.startApplication(appPath)
    Windows.delay(3)

    KeywordUtil.logInfo('TC-01: Autentificare pacient')
    Windows.setText(findWindowsObject('Login/Input_Email'), pacientEmail)
    Windows.setText(findWindowsObject('Login/Input_Parola'), pacientParola)
    Windows.click(findWindowsObject('Login/Button_Autentificare'))
    assert Windows.verifyElementPresent(findWindowsObject('Pacient/Dashboard/Titlu_Pacient'), 10)

    KeywordUtil.logInfo('TC-02: Navigare la Fisa medicala')
    Windows.click(findWindowsObject('Pacient/Meniu/Button_FisaMedicala'))
    assert Windows.verifyElementPresent(findWindowsObject('Pacient/Fisa/Titlu_FisaMedicala'), 10)

    KeywordUtil.logInfo('TC-03: Selectare consultatie din istoric')
    Windows.click(findWindowsObject('Pacient/Fisa/Istoric/Prima_Consultatie'))
    assert Windows.verifyElementPresent(findWindowsObject('Pacient/Fisa/TextBox_Simptome'), 10)
    assert Windows.verifyElementPresent(findWindowsObject('Pacient/Fisa/TextBox_Diagnostic'), 10)

    KeywordUtil.logInfo('TC-04: Solicitare reinnoire reteta')
    Windows.click(findWindowsObject('Pacient/Meniu/Button_ReteteActive'))
    assert Windows.verifyElementPresent(findWindowsObject('Pacient/Retete/Card_PrimaReteta'), 10)
    Windows.click(findWindowsObject('Pacient/Retete/Card_PrimaReteta'))
    Windows.click(findWindowsObject('Pacient/Retete/Button_SolicitaReinnoire'))
    assert Windows.verifyElementPresent(findWindowsObject('Pacient/Retete/Mesaj_SolicitareTrimisa'), 10)

    KeywordUtil.markPassed('Testele automate pentru pacient au fost executate cu succes.')
} catch (Throwable error) {
    KeywordUtil.markFailed('Testul automat MediClin a esuat: ' + error.getMessage())
    throw error
} finally {
    Windows.closeApplication(FailureHandling.OPTIONAL)
}

