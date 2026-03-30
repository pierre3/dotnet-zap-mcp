#!/bin/sh

# First run: copy template config; subsequent runs: update API key only
if [ ! -f /home/zap/.ZAP/config.xml ]; then
    mkdir -p /home/zap/.ZAP
    cp /zap/wrk/template/zap-config.xml /home/zap/.ZAP/config.xml
    sed -i "s/ZAP_API_KEY_PLACEHOLDER/${ZAP_API_KEY}/g" /home/zap/.ZAP/config.xml
else
    sed -i "s|<key>.*</key>|<key>${ZAP_API_KEY}</key>|" /home/zap/.ZAP/config.xml
fi

# Ensure shared directories exist for reports, sessions, and contexts
mkdir -p /zap/wrk/data/reports
mkdir -p /zap/wrk/data/sessions
mkdir -p /zap/wrk/data/contexts

exec "$@"
