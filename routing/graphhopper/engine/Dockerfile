FROM eclipse-temurin:17-jre

WORKDIR /app

COPY graphhopper-web-12.0-SNAPSHOT.jar ./graphhopper.jar

CMD ["java", "-Ddw.graphhopper.datareader.file=../data/czech-republic-latest.osm.pbf", "-jar", "graphhopper.jar", "server", "../graphhopper-config/config.yml"]