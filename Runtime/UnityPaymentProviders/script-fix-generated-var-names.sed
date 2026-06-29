# replace "var unity.consent_state." with "unity.consent_state_"
s/var unity.consent_state\./var unity_consent_state_/g

# replace "var unity." with "var unity_"
s/var unity\./var unity_/g

# replace "var session." with "var session_"
s/var session\./var session_/g


# replace '", unity.consent_state.' with '", unity_consent_state_'
s/", unity\.consent_state\./", unity_consent_state_/g

# replace '", unity.' with '", unity_'
s/", unity\./", unity_/g

# replace '", session.' with '", session_'
s/", session\./", session_/g
